using System;
using RabbitMQ.Client;
using System.Net.Sockets;
using NModbus;
using System.Text;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Collections;

namespace prdocer
{
    public class rabbitconnect
    {
        static rabbitconnect staticrabbit;
        ConnectionFactory rfactory; // 전역 변수
        IConnection rconnection;
        IChannel rchannel;
        public rabbitconnect()
        {
            this.rfactory = null;
            this.rconnection = null;
            this.rchannel = null;
        }
        static public rabbitconnect Instance()
        {
            if(staticrabbit == null)
            {
                staticrabbit = new rabbitconnect();
            }
            return staticrabbit;
        }
        private async void makeConnetion()
        {
            this.rfactory = new ConnectionFactory()
            {
                HostName = "211.187.0.113",
                UserName = "guest",
                Password = "guest",
                Port = 5672
            }; // rabbitmq(라즈베리 파이에 존재) 연결
        }

        public async Task connect()
        {
            // 연결이 없으면 연결 시도
            if(this.rfactory == null || this.rconnection == null || this.rchannel == null)
            {
                try
                {
                    // rabbitmq 서버 연결
                    this.makeConnetion(); // factory 연결 
                    this.rconnection = await this.rfactory.CreateConnectionAsync();
                    this.rchannel = await this.rconnection.CreateChannelAsync();
                    // await this.rchannel.ExchangeDeclareAsync(exchange: "alarm", type: ExchangeType.Fanout); // 새 exchange 생성

                    // [1] PLC 접속 정보 (쓰기 때와 동일하게)
                    string plcIp = "192.168.10.200";
                    int plcPort = 502;   // 기본 ModbusTCP 포트
                    byte slaveId = 1;    // PLC에서 설정한 Slave ID

                    // 읽어올 시작 주소와 레지스터 개수
                    ushort startAddress = 0;
                    ushort numRegistersToRead = 10;

                    // 동일 알람 전송 방지 장치
                    int[] alarmFlagLOW = new int[9];
                    int[] alarmFlagHIGH = new int[9];
                    double[] regs = new double[9];

                    double[] LOW = new double[] { 70.0, 90, 5.5, 180, 350, 15, 35, 1.8, 180 };
                    double[] HIGH = new double[] { 95.0, 320, 6.1, 320, 850, 30, 65, 2.2, 660 };
                    string[] Procedure = new string[] { "수위", "점도", "PH", "전압", "전류", "온도", "습도", "스프레이 건 공압", "페인트 유량" };
                    
                    using (TcpClient client = new TcpClient(plcIp, plcPort))
                    {
                        var tcpfactory = new ModbusFactory();
                        IModbusMaster tcpmaster = tcpfactory.CreateMaster(client);
                        while (true)
                        {
                            ushort[] registers = tcpmaster.ReadHoldingRegisters(slaveId, startAddress, numRegistersToRead);
                            // b) 읽은 ushort[]를 센서값으로 변환
                            //    (쓰는 쪽에서 GenerateRegisterData()와 동일한 포맷으로 해석)
                            // reg9 = registers[9]; // 예비/확장
                            regs[0] = (double)registers[0]; // level(%)
                            regs[1] = (double)registers[1]; // 점도(cP)
                            regs[2] = (double)(registers[2] / 100.0); // PH
                            regs[3] = (double)registers[3]; // 전압(V)
                            regs[4] = (double)registers[4]; // 전류(A)
                            regs[5] = (double)registers[5]; // 온도
                            regs[6] = (double)registers[6]; // 습도(%)
                            regs[7] = (double)(registers[7] / 100.0); // bar 
                            regs[8] = (double)registers[8]; // mL/min
                            // (1) PreTreatment
                            //double level = reg0;        // Level (%)
                            //double viscosity = reg1;        // 점도 (cP)
                            //double pH = reg2 / 100.0; // pH는 x100 스케일링
                            //double voltage = reg3;        // 전압 (V)
                            //double current = reg4;        // 전류 (A)
                            // (2) Drying
                            //double temperature = reg5;      // 온도 (°C)
                            //double humidity = reg6;      // 습도 (%)
                            // (3) Painting
                            //double paintPressure = reg7 / 100.0; // bar는 x100 스케일링
                            //double paintFlow = reg8;         // mL/min


                            // 메세지 생성
                            string NOW = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                            string message = $"{NOW}, {regs[0]}, {regs[1]}, {regs[2]}, {regs[3]}, {regs[4]}, {regs[5]}, {regs[6]}, {regs[7]}, {regs[8]}";
                            var body = Encoding.UTF8.GetBytes(message);

                            await this.rchannel.BasicPublishAsync(exchange: "logs", routingKey: string.Empty, body: body);
                            Console.WriteLine($" [x] Sent {message}");
                            // 임계치 초과, 미만 검사하기
                            // false면 큐에 알람 넣기, true면 x
                            // 센서 번호, 센서 값, 임계치 이하, 임계치 이상인지?
                            for (int i = 0; i < 9; i++)
                            {
                                // 임계치 검사 미만
                                if (regs[i] < LOW[i])
                                {
                                    string message2 = $"{NOW}, {Procedure[i]} 센서 {regs[i]}, LOW";
                                    var body2 = Encoding.UTF8.GetBytes(message2);
                                    await this.rchannel.BasicPublishAsync(exchange: "alarm", routingKey: string.Empty, body: body2);
                                    //alarmFlagLOW[i]++;
                                }
                                if (regs[i] > HIGH[i])
                                {
                                    string message2 = $"{NOW}, {Procedure[i]} 센서 {regs[i]}, HIGH";
                                    var body2 = Encoding.UTF8.GetBytes(message2);
                                    await this.rchannel.BasicPublishAsync(exchange: "alarm", routingKey: string.Empty, body: body2);
                                    //alarmFlagHIGH[i]++;
                                }

                                // 알람 후 1분 동안 동일 알람 발생 방지
                                //if (alarmFlagHIGH[i] > 0) alarmFlagHIGH[i] = (alarmFlagHIGH[i] + 1) % 61;
                                //if (alarmFlagLOW[i] > 0) alarmFlagLOW[i] = (alarmFlagLOW[i] + 1) % 61;
                            }
                            
                            Thread.Sleep(1000);
                        }
                    }
                }
                catch(Exception e)
                {
                    Console.WriteLine($"연결 실패 다시 시도 : {e.ToString()}");
                }
            }  // 컨슈머가 큐를 생성함?
        }

        string inspect_standard(double num, double row, double high)
        {
            string message = "";
            if(num < row)
            {
                message = $"{num}, Low";
            }
            else
            {
                message = $"{num}, High";
            }
            return message;
        }
    }
}

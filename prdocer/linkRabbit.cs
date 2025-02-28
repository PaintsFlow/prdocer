using System;
using RabbitMQ.Client;
using System.Net.Sockets;
using NModbus;
using System.Text;

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

                    // [1] PLC 접속 정보 (쓰기 때와 동일하게)
                    string plcIp = "192.168.10.200";
                    int plcPort = 502;   // 기본 ModbusTCP 포트
                    byte slaveId = 1;    // PLC에서 설정한 Slave ID

                    // 읽어올 시작 주소와 레지스터 개수
                    ushort startAddress = 0;
                    ushort numRegistersToRead = 10;

                    using(TcpClient client = new TcpClient(plcIp, plcPort))
                    {
                        var tcpfactory = new ModbusFactory();
                        IModbusMaster tcpmaster = tcpfactory.CreateMaster(client);
                        while (true)
                        {
                            ushort[] registers = tcpmaster.ReadHoldingRegisters(slaveId, startAddress, numRegistersToRead);
                            // b) 읽은 ushort[]를 센서값으로 변환
                            //    (쓰는 쪽에서 GenerateRegisterData()와 동일한 포맷으로 해석)
                            int reg0 = registers[0];
                            int reg1 = registers[1];
                            int reg2 = registers[2];
                            int reg3 = registers[3];
                            int reg4 = registers[4];
                            int reg5 = registers[5];
                            int reg6 = registers[6];
                            int reg7 = registers[7];
                            int reg8 = registers[8];
                            // reg9 = registers[9]; // 예비/확장

                            // (1) PreTreatment
                            double level = reg0;        // Level (%)
                            double viscosity = reg1;        // 점도 (cP)
                            double pH = reg2 / 100.0; // pH는 x100 스케일링
                            double voltage = reg3;        // 전압 (V)
                            double current = reg4;        // 전류 (A)

                            // (2) Drying
                            double temperature = reg5;      // 온도 (°C)
                            double humidity = reg6;      // 습도 (%)

                            // (3) Painting
                            double paintPressure = reg7 / 100.0; // bar는 x100 스케일링
                            double paintFlow = reg8;         // mL/min
                                                             //string message = $"10, 10, 10, 10, 10, 10, 10, 10, 10, 10";
                                                             //var body = Encoding.UTF8.GetBytes(message);

                            string message = $"{level}, {viscosity}, {pH}, {voltage}, {current}, {temperature}, {humidity}, {paintPressure}, {paintFlow}";
                            //string message = $"10, 10, 10, 10, 10, 10, 10, 10, 10, 10";
                            var body = Encoding.UTF8.GetBytes(message);

                            await this.rchannel.BasicPublishAsync(exchange: "logs", routingKey: string.Empty, body: body);
                            Console.WriteLine($" [x] Sent {message}");
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
    }
}

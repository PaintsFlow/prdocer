using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

using NModbus;
using NModbus.Device;

namespace prdocer
{
    internal class plcRead
    {
        void connect()
        {
            // PLC 접속 정보
            string plcIp = "192.168.10.200";
            int plcPort = 502;   // 기본 ModbusTCP 포트
            byte slaveId = 1;    // PLC에서 설정한 Slave ID

            // 읽어올 시작 주소와 레지스터 개수
            ushort startAddress = 0;
            ushort numRegistersToRead = 10;

            try
            {
                TcpClient client = new TcpClient(plcIp, plcPort);
                
            }
            catch
            {
                Console.WriteLine("PLC 연결 불가");
            }
        }
    }
}

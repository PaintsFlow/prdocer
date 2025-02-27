using System;
using System.Text;
using prdocer;
using RabbitMQ.Client;

namespace Producer
{
    class SensorProduce // plc 데이터를 뿌리고 DB에 5분마다 저장하는 클래스
    {

        static async Task Main(string[] args)
        {
            rabbitconnect tmp = new rabbitconnect();
            Task x = tmp.connect();
            x.Wait();
        }
    }
}
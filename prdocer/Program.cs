using System;
using System.Text;
using prdocer;
using RabbitMQ.Client;

namespace Producer
{
    class SensorProduce // plc 데이터를 뿌리고 DB에 5분마다 저장하는 클래스
    {
        // 1. PLC 데이터 읽어 오기
        // 2. rabbitmq 전달하기
        // 3. DB 저장
        static async Task Main(string[] args)
        {
            // rabbit mq 사용
            rabbitconnect tmp = rabbitconnect.Instance(); // singleton 사용
            Task x = tmp.connect();
            x.Wait();
        }
    }
}
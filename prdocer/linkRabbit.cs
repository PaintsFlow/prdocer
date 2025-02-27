using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using RabbitMQ.Client; 

namespace prdocer
{
    public class rabbitconnect
    {
        ConnectionFactory factory; // 전역 변수
        IConnection connection;
        IChannel channel;
        public rabbitconnect()
        {
            this.factory = null;
            this.connection = null;
            this.channel = null;
        }
        private async void  makeConnetion()
        {
            this.factory = new ConnectionFactory()
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
            if(this.factory == null || this.connection == null || this.channel == null)
            {
                try
                {
                    this.makeConnetion(); // factory 연결 
                    this.connection = await this.factory.CreateConnectionAsync();
                    this.channel = await this.connection.CreateChannelAsync();


                    while (true)
                    {
                        string message = $"10, 10, 10, 10, 10, 10, 10, 10, 10, 10";
                        var body = Encoding.UTF8.GetBytes(message);

                        await this.channel.BasicPublishAsync(exchange: "logs", routingKey: string.Empty, body: body);
                        Console.WriteLine($" [x] Sent {message}");

                        Thread.Sleep(1500);

                    }
                }
                catch
                {
                    Console.WriteLine("연결 실패 다시 시도");
                }
                
            }  // 컨슈머가 큐를 생성함?
        }
        //var body = Encoding.UTF8.GetBytes(message);
        //await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "hello", body: body);
    }
}

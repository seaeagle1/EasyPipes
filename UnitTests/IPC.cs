using System;
using System.IO;
using EasyPipes;
using Xunit;
using ExpectedObjects;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;

namespace EPUnitTests
{
    public class IPC
    {
        private IpcMessage testMessage = new IpcMessage()
        {
            Service = "ServiceName",
            Method = "TestMethod",
            Parameters = new object[]
            {
                5, "test string", new IpcMessage()
            }
        };

        [Fact]
        public void CheckSerialization()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                var inMessage = testMessage.ToExpectedObject();

                IpcStream ipcStream = new IpcStream(ms, new List<Type>());

                ipcStream.WriteMessage(testMessage);
                ms.Seek(0, SeekOrigin.Begin);
                IpcMessage returnMessage = ipcStream.ReadMessage();

                inMessage.ShouldEqual(returnMessage);
            }
        }

        public interface IService
        {
            int Sum(int one, int two);
            int Count(List<object> array);
            int GetNumberOfCalls();

            int Property { get; set; }
        }

        class Calculator : IService
        {
            int number_of_calls = 0;

            public int Sum(int one, int two)
            {
                number_of_calls++;
                return one + two;
            }

            public int Count(List<object> array)
            {
                return array.Count;
            }

            public int GetNumberOfCalls()
            {
                return number_of_calls;
            }

            public int Property
            {
                get; set;
            }
        }

        [Fact]
        public void CheckMessageProcessing()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // prep
                IpcMessage message = new IpcMessage()
                {
                    Service = "IService",
                    Method = "Sum",
                    Parameters = new object[] { 3, 5 }
                };

                IpcStream ipc = new IpcStream(ms, new List<Type>());

                ipc.WriteMessage(message);
                long startpos = ms.Position;
                ms.Seek(0, SeekOrigin.Begin);
                // ----


                Server s = new Server("testpipe");
                s.RegisterService<IService>(new Calculator());
                s.ProcessMessage(ms, new Guid());


                // post
                ms.Seek(startpos, SeekOrigin.Begin);
                IpcMessage returnMessage = ipc.ReadMessage();

                var expectedMessage = new IpcMessage()
                {
                    Return = 8
                }.ToExpectedObject();

                expectedMessage.ShouldEqual(returnMessage);
            }
        }

        [Fact]
        public void ClientServerTest()
        {
            Server server = new Server("testpipe2");
            server.RegisterService<IService>(new Calculator());
            server.Start();

            Client client = new Client("testpipe2");
            IService service = client.GetServiceProxy<IService>();
            int result = service.Sum(6, 12);

            server.Stop();

            Assert.Equal(18, result);
        }

        [Fact]
        public void TcpTest()
        {
            Server server = new TcpServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8055));
            server.RegisterService<IService>(new Calculator());
            server.Start();

            Client client = new TcpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8055));
            IService service = client.GetServiceProxy<IService>();
            int result = service.Sum(9, 11);

            server.Stop();

            Assert.Equal(20, result);
        }

        [Fact]
        public void ObjectPassage()
        {
            Server server = new Server("testpipe3");
            server.RegisterService<IService>(new Calculator());
            server.Start();

            Client client = new Client("testpipe3");
            IService service = client.GetServiceProxy<IService>();

            List<object> list = new List<object>();
            list.Add(new IpcMessage());
            list.Add(new IpcMessage());
            int result = service.Count(list);

            server.Stop();

            Assert.Equal(2, result);
        }

        [Fact]
        public void MultipleMessageTest()
        {
            Server server = new Server("testpipe4");
            server.RegisterService<IService>(new Calculator());
            server.Start();

            Client client = new Client("testpipe4");
            IService service = client.GetServiceProxy<IService>();

            if (!client.Connect())
                return;

            List<int> results = new List<int>();

            results.Add(service.Sum(6, 12));
            results.Add(service.Sum(4, 6));
            results.Add(service.Sum(2, 2));

            server.Stop();

            Assert.Equal(new int[] { 18, 10, 4 }, results);
        }

        [Fact]
        public void StatefulTcpTest()
        {
            Server server = new TcpServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8055));
            server.RegisterStatefulService<IService>(typeof(Calculator));
            server.Start();

            Client client = new TcpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8055));
            client.Connect();

            IService service = client.GetServiceProxy<IService>();
            service.Sum(9, 11);
            service.Sum(2, 4);

            int result = service.GetNumberOfCalls();

            client.Disconnect();
            server.Stop();

            Assert.Equal(2, result);
        }

        [Fact]
        public void StatefulTcpTest2()
        {
            Server server = new TcpServer(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8055));
            server.RegisterStatefulService<IService>(typeof(Calculator));
            server.Start();

            Client client = new TcpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8055));
            client.Connect();
            IService service = client.GetServiceProxy<IService>();

            Client client2 = new TcpClient(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8055));
            client2.Connect();
            IService s2 = client2.GetServiceProxy<IService>();

            service.Sum(9, 11);
            s2.Sum(3, 5);
            service.Sum(2, 4);

            int result = service.GetNumberOfCalls();

            client2.Disconnect();
            client.Disconnect();
            server.Stop();

            Assert.Equal(2, result);
        }

        [Fact]
        public void PropertyTest()
        {
            Server server = new Server("testpipe5");
            Calculator calc = new Calculator();
            server.RegisterService<IService>(calc);
            server.Start();

            Client client = new Client("testpipe5");
            IService service = client.GetServiceProxy<IService>();

            service.Property = 5;
            int result = service.Property;

            server.Stop();

            Assert.Equal(5, calc.Property);
            Assert.Equal(calc.Property, result);
        }
    }
}

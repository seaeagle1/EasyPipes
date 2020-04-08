# EasyPipes
Simple C# library for NamedPipe and TCP-based IPC

Written against .NET Standard 2.0, ought to be compatible with most modern .NET versions.

## Usage

Server usage:
```csharp
using EasyPipes;

// IService defines the IPC interface
public interface IService
{
   int Sum(int one, int two);
}

// Calculator is the server class
class Calculator : IService
{
   public int Sum(int one, int two)
   {
      return one + two;
   }
}

// start up
Server server = new Server("pipename");
server.RegisterService<IService>(new Calculator());
server.Start();

// eventual shutdown
server.Stop();
```

Client usage:
```csharp
using EasyPipes;

// IService defines the IPC interface
public interface IService
{
   int Sum(int one, int two);
}

// setup client
Client client = new Client("pipename");
IService service = client.GetServiceProxy<IService>();

// execute remote operation
int result = service.Sum(6, 12); // = 18
```

Encrypted TCP:
```csharp
using EasyPipes;

/// IService defines the IPC interface
public interface IService
{
	[EncryptIfTrue]
	bool Authenticate(string username, string password);

	string GetSecretData();
}

// --------- SERVER --------------
class StatefulService : IService
{
	private bool has_authenticated = false;

	public bool Authenticate(string username, string password)
	{
		if(Authenticated(username, password))
		{
			this.has_authenticated = true;
			return true;
		} else {
			return false;
		}
	}

	public string GetSecretData()
	{
		if(has_authenticated)
			return somesecretstuff;
		else
			throw new Exception("Not authenticated");
	}
}

// start up
TcpServer server = new TcpServer(new IPEndPoint(address, port), new Encryptor(sharedkey));
server.RegisterStatefulService<IService>(typeof(StatefulService));
server.Start();

// eventual shutdown
server.Stop();

// --------- CLIENT -------------
// setup client
TcpClient client = new TcpClient(new IPEndPoint(address, port), new Encryptor(sharedkey));
IService service = client.GetServiceProxy<IService>();

// authenticate (transmitted in plaintext!)
if(service.Authenticate(username, password))
{
	// transmit encrypted messages
	string data = service.GetSecretData();
}
```

Remarks:
* By default there is a single instance of the server class used by all clients! Use the stateful service for service which require an instance per connection.
* It's primarily focussed on IPC on single-machine or swift LAN-connections, so it likely will not perform well with long-distance internet connections.
* Any and all arguments or return values are serialized using DataContractSerializer, so custom types that need to cross the connection should be designed accordingly.
* Provides optional AES256 encryption for TCP connections upon call to a labelled service method, for example after authentication

## Licence

Mozilla Public Licence 2.0

In simple terms: You can use the library as-is for any of your own projects, but any changes to the library itself have to be available under the same terms.

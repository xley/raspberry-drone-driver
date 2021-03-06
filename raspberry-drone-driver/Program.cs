using System;
using Raspberry.IO.GeneralPurpose;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Linq;
using System.Diagnostics;


namespace RaspberryDroneDriver
{
	

	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello io!" );

//			TestServer ();


			TestGpio ();

		}

		static async void TestServer(  )
		{
			var listener = new TcpListener ( IPAddress.Any, 80 );
			listener.Start ();

			while (true)
			{
				var client = await listener.AcceptTcpClientAsync ();

				Task.Run (async() => {

					using (var stream = client.GetStream ())
					using (var reader = new StreamReader (stream))
					using (var writer = new StreamWriter (stream)) 
					{
						List<string> requestHeadder = new List<string> ();

						while (true) {
							var line = await reader.ReadLineAsync ();
							if (String.IsNullOrWhiteSpace (line))
								break;
							requestHeadder.Add (line);
						}

						var requestLine = requestHeadder.FirstOrDefault ();
						var requestParts = requestLine?.Split (new[]{ ' ' }, 3);
						if (!requestHeadder.Any () || requestParts.Length != 3) {
							await writer.WriteLineAsync ("HTTP/1.0 400 Bad Request");
							await writer.WriteLineAsync ("Content-Type: text/plain; charset=UTF-8");
							await writer.WriteLineAsync ();
							await writer.WriteLineAsync ("Bad request");
							return;
						}

						Console.WriteLine (client.Client.RemoteEndPoint);

						var path = requestParts [1];
						if (path == "/") {
							await writer.WriteLineAsync ("HTTP/1.0 200 OK");
							await writer.WriteLineAsync ("Content-Type: text/plain; charset=UTF-8");
							await writer.WriteLineAsync ();
							await writer.WriteLineAsync ("here is root");
						} else if (path == "/test") {
							await writer.WriteLineAsync ("HTTP/1.0 200 OK");
							await writer.WriteLineAsync ("Content-Type: text/plain; charset=UTF-8");
							await writer.WriteLineAsync ();
							await writer.WriteLineAsync ("here is test");
						}
					} 

				} );

			}
//			Console.ReadLine ();
		}


		// 37	out vcc immediate

		// 35	out	B Enable2
		// 33	out	B Phase1 

		// 31	out	A Enable2
		// 29	out	A Phase1
		static async void TestGpio()
		{
			//var inputConfig = ConnectorPin.P1Pin15.Input ();
			//inputConfig.OnStatusChanged (status => {Console.WriteLine (status);});


			var connection = new GpioConnection ( ConnectorPin.P1Pin29.Output (),
			                                     ConnectorPin.P1Pin31.Output (),

			                                     ConnectorPin.P1Pin33.Output (),
			                                     ConnectorPin.P1Pin35.Output (),

			                                     ConnectorPin.P1Pin37.Output (),
																												ConnectorPin.P1Pin36.Output (),
																												ConnectorPin.P1Pin38.Output ()
																											);

			var connected29 = connection.Pins [ConnectorPin.P1Pin29];
			var connected31 = connection.Pins [ConnectorPin.P1Pin31];

			var connected33 = connection.Pins [ConnectorPin.P1Pin33];
			var connected35 = connection.Pins [ConnectorPin.P1Pin35];

			var connected37 = connection.Pins [ConnectorPin.P1Pin37];

			var connected36 = connection.Pins [ConnectorPin.P1Pin36];	// survo signal2
			var connected38 = connection.Pins [ConnectorPin.P1Pin38];	// survo signal1

			connected37.Enabled = true;	// vcc on

			var stopwatch = new Stopwatch ();
			stopwatch.Start ();

			var yawPitchSurvo = new YawPitchSurvo ((flag, counnt) => {
				connected38.Enabled = flag;	// yaw
			}, 
				(flag, count) => {
					connected36.Enabled = flag;	// pitch
			});



			var rightWheel = new WheelDriver ( phasePin: connected29, enablePin:connected31 );
			var leftWheel = new WheelDriver ( phasePin: connected33, enablePin:connected35 );
			var dualWheel = new DualWheelPwmDriver( leftWheel, rightWheel );
			var tank = new TankDriver( dualWheel );



				var task = Task.Run(async() => {
				while(true)
				{
					yawPitchSurvo.Update();

					if( Console.KeyAvailable )
					{
						var res = Console.ReadKey(true);
						switch( res.Key )
						{
						case ConsoleKey.Escape:
						case ConsoleKey.Q:
							Console.WriteLine (" console exit");
							return;

						case ConsoleKey.P:
							yawPitchSurvo.Fold();
							Console.WriteLine (" Fold");
							break;

						case ConsoleKey.J:
							yawPitchSurvo.StepYawLeft(false);
							Console.WriteLine (" StepYawLeft");
							break;
						case ConsoleKey.L:
							yawPitchSurvo.StepYawRight(false);
							Console.WriteLine (" StepYawRight");
							break;
						case ConsoleKey.I:
							yawPitchSurvo.StepPitchDown(false);
							Console.WriteLine (" StepPitchDown");
							break;
						case ConsoleKey.K:
							yawPitchSurvo.StepPitchUp(false);
							Console.WriteLine (" StepPitchUp");
							break;

						case ConsoleKey.S:
						case ConsoleKey.DownArrow:
							tank.ReverseAccelerate();
							Console.WriteLine ("console back "+ tank.powerText );
							break;
						case ConsoleKey.W:
						case ConsoleKey.UpArrow:
							tank.Accelerate();
							Console.WriteLine ("console fore "+ tank.powerText);
							break;
						case ConsoleKey.A:
						case ConsoleKey.LeftArrow:
							tank.TurnLeft();
							Console.WriteLine ("console left "+ tank.powerText);
							break;
						case ConsoleKey.D:
						case ConsoleKey.RightArrow:
							tank.TurnRight();
							Console.WriteLine ("console right "+ tank.powerText);
							break;
						case ConsoleKey.Spacebar:
							tank.Brake();
							Console.WriteLine ("console Spacebar");
							break;
						}
					}
					await Task.Delay(100);
//					await Task.Yield();
				}

			} );

			await task;

			connection.Close ();

			yawPitchSurvo.Dispose ();
		}




		static void InputTest( InputPinConfiguration config )
		{
			var connection = new GpioConnection (config);
//			connection.PinStatusChanged += (sender, e) => {
//				Console.WriteLine ("PinStatusChanged() : "+e.Enabled );
//			};

			for (int i=0; i<10000; i++) 
			{
				Console.WriteLine (connection.Pins[config].Enabled );
				System.Threading.Thread.Sleep (10);
			}


			connection.Close ();	

		}


	}
}

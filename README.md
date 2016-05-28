# ViSiGenie4DSystems.Async

# SUMMARY

This is a C# async class library that provides host-to-display serial communications for Windows IoT apps requiring a 4D Systems non-primary touch display.
For example, one of many kits like the 4.3" DIABLO16 Display Module with 4GB Industrial Grade MicroSD Card and Silicon Labs CP2102 USB to Serial UART Bridge Converter Cable.

4D Systems Windows IoT makers and commercial builders using this library will appreciate how nicely its object-oriented library maps to the ViSi-Genie Communication Protocols, Objects, Properties, and Genie Magic concepts written in the ViSi-Genie Reference Manual.

* Use the ViSiGenie4DSystems.Async library to support host application development on the Raspberry Pi 2 and 3, Arrow DragonBoard 410c or MinnowBoard MAX app. 
  Hereafter, the Raspberry Pi 2 and 3, Arrow DragonBoard 410c or MinnowBoard MAX is simply referred to as the "Host". In fact, the
  underlying C# class supports host -to- 4D Display serial communications is called Host.

* The ViSiGenie4DSystems.Async library supports discovery of one or more connected serial devices. 
  A scalability feature that bodes well for using the Silicon Labs 4D Systems Programming Cable is this software supports display discovery and instantiate instances of itself for each serially connected device. For example, the Raspberry Pi 2 has four USB ports and this means four different 4D Systems display modules could be connected up to a single Pi device. 
  
* The ViSiGenie4DSystems.Async library supports listening for ViSi Genie Report Events. 
  This is where the host app can receive Report Event and Report Object Messages from 4D Display. 
  i.e., the user presses a button object.
  
* This library is NOT a C Language port of the existing 4D Systems Linux Raspberry Pi ViSi-Genie or Raspberry Pi Serial code as these libraries are incompatible with the asynchronous programming model. 

* A Windows IoT Core device can be configured to run a single headed or headless application. 
  Likewise, a Headed or Headless app can use a 4D Display also. 

## QUICK START

* Deploy your 4D Workshop4 project to your 4D Systems display's uSD card. 

* Edit your app's package manifest and add serialcommunication capability; otherwise serial communications will fail when you try to connect to the 4D Systems display.

	<Capabilities>
		<DeviceCapability Name="serialcommunication">
			<Device Id = "any" >
				<Function Type="name:serialPort" />
			</Device>
		</DeviceCapability>
	</Capabilities>
		
* Use the 4D Systems Silabs USB programmers cable. Connect the cable from the host USB port to display's backside 5 pins connector.

Here is a Windows IoT headless app example, which highlights how to use the ViSiGenie4DSystems.Async library:

```C#
using System;
using System.Linq;
using Windows.ApplicationModel.Background;
using System.Diagnostics;
using Windows.System;
using System.Threading.Tasks;
using System.Threading;

using ViSiGenie4DSystems.Async.Enumeration;
using ViSiGenie4DSystems.Async.Message;
using ViSiGenie4DSystems.Async.SerialComm;

namespace DisplayHeadless
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral _defferal;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {		
			var cts = new CancellationTokenSource();
		
			//1. Find the connected device's identifier string and hang on to it
			Task<List<string>> discoverDeviceIdsTask = Host.Instance.DiscoverDeviceIds();
			
			await discoverDeviceIdsTask;
			 
			//2. Connect host to 4D Systems display
			var deviceId = discoverDeviseIdsTask.Result.First();
			
			//Baud rate of host must match 4D Workshop project baud rate
			var portDef = new PortDef(BaudRate.Bps115200);
			Task connectTask = Host.Instance.Connect(deviceId, portDef);
			await connectTask;

			//3. Host start listening for display events
			await Host.Instance.StartListening(deviceId,
												/*** TODO MyReportEventClass.ReportEventMessageHandler.Handler,
												MyReportObjectClass.ReportObjectStatusMessageHandler.Handler ***/);
		
			//4. Change to form 0 on display per 4D Workshop4 project layout...
			const int displayFormId = 0;
			var writeObjectMessage = new WriteObjectValueMessage(ObjectType.Form, displayFormId);
			await Host.Instance.Send(enabledBoard.SerialDeviceId, writeObjectMessage, cts.Token);

			//5. Write string message to display per 4D Workshop4 project layout...
			const int displayStringId = 0;
			var writeStringMessage = new WriteStringASCIIMessage(displayStringId, "Hello 4D Systems via Windows IoT!");
			await Host.Instance.Send(enabledBoard.SerialDeviceId, writeStringMessage, cts.Token);
		
			//5. Host stops listening to display events
		    Host.Instance.StopListening(enabledBoard.Value.SerialDeviceId,
										/*** TODO MyReportEventClass.ReportEventMessageHandler.Handler,
										MyReportObjectClass.ReportObjectStatusMessageHandler.Handler ***/);
    
            _defferal.Complete();
        }        
    }
}
```

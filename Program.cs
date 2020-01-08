using Forge.Logging;
using Forge.Logging.Log4net;
using Sesame.Communication.Data.Indentification;
using Sesame.Communication.External.Client;
using System;
using System.Configuration;
using System.ServiceModel;
using System.Threading;

namespace NETCoreConsoleAppWithServerCalls
{
    class Program
    {

        private static ILog LOGGER = null;

        private readonly AutoResetEvent mFaultHandlerEvent = new AutoResetEvent(false);
        private Thread mFaultHandlerThread = null;
        private AutoResetEvent mFaultHandlerStopEvent = null;
        private bool mFaultHandlerRunning = true;


        static void Main(string[] args)
        {
            // initializing logger
            Log4NetManager.InitializeFromAppConfig();
            LOGGER = LogManager.GetLogger(typeof(Program));
            LogUtils.LogAll();



            Console.WriteLine("Running tests...");
            
            Program p = new Program();
            p.Initialize();
            p.RunTestCalls();
            p.Shutdown();

            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }

        public void Initialize()
        {
            // create wcf binding and endpoint address
            ClientProxyBase.SourceId = ClientIdGenerator.GenerateId(ClientTypeEnum.External);
            NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
            binding.Name = "TcpEndpoint";
            binding.OpenTimeout = TimeSpan.FromMinutes(1);
            binding.CloseTimeout = TimeSpan.FromMinutes(1);
            binding.ReceiveTimeout = TimeSpan.FromMinutes(10);
            binding.SendTimeout = TimeSpan.FromMinutes(10);
            binding.MaxBufferSize = 2147483647;
            binding.MaxReceivedMessageSize = 2147483647;
            binding.TransferMode = TransferMode.Buffered;
            binding.ReaderQuotas.MaxDepth = 2147483647;
            binding.ReaderQuotas.MaxStringContentLength = 2147483647;
            binding.ReaderQuotas.MaxArrayLength = 2147483647;
            binding.ReaderQuotas.MaxBytesPerRead = 2147483647;
            binding.ReaderQuotas.MaxNameTableCharCount = 2147483647;
            binding.Security.Mode = SecurityMode.None;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.None;
            binding.Security.Transport.ClientCredentialType = TcpClientCredentialType.Certificate;

            EndpointAddress endpoint = new EndpointAddress(ConfigurationManager.AppSettings["SesameServiceUrl"]);

            // initialize communication system
            ClientProxyBase.ConfigureClientProxyForCallback(new ConfigurationForCallback(binding, endpoint));
            ClientProxyBase.Faulted += ClientProxyBase_Faulted;
            try
            {
                ClientProxyBase.Open();
            }
            catch (Exception ex)
            {
                LOGGER.Error(string.Format("Failed to open connection. Reason: {0}", ex.Message));
                mFaultHandlerEvent.Set();
            }
            mFaultHandlerThread = new Thread(new ThreadStart(FaultHandlerThreadMain));
            mFaultHandlerThread.Name = "FaultHandlerThread";
            mFaultHandlerThread.Start();
        }

        public void RunTestCalls()
        {
            using (ComProxy proxy = new ComProxy())
            {
                // query database(s)
                DatabaseResponse dbResponse = proxy.GetDatabases();
                foreach (KeyAndValueItem item in dbResponse.Items)
                {
                    LOGGER.Info($"DbId: {item.Id}, Name: {item.Name}");
                    // query each db details
                    SPDatabaseDetailsResponse dbDetails = proxy.GetDatabaseDetails(new SPDatabaseDetailsRequest() { DatabaseId = item.Id });
                    LOGGER.Info($"Currency: {dbDetails.Currency}");
                    LOGGER.Info($"Population: {dbDetails.Population.ToString()}");
                    LOGGER.Info($"Sample: {dbDetails.Sample.ToString()}");
                    LOGGER.Info("Languages:");
                    foreach (KeyAndValueItem langItem in dbDetails.Languages)
                    {
                        LOGGER.Info($"{langItem.Id}, name: {langItem.Name}");
                    }
                    LOGGER.Info("-----------");
                }
            }
            Console.WriteLine("Success! Check your log file.");
        }

        public void Shutdown()
        {
            ClientProxyBase.Faulted -= ClientProxyBase_Faulted;

            mFaultHandlerStopEvent = new AutoResetEvent(false);
            mFaultHandlerRunning = false;
            mFaultHandlerEvent.Set();
            mFaultHandlerStopEvent.WaitOne();
            mFaultHandlerStopEvent.Dispose();
            mFaultHandlerEvent.Dispose();

            ClientProxyBase.Close();
        }

        private void ClientProxyBase_Faulted(object sender, EventArgs e)
        {
            mFaultHandlerEvent.Set();
        }

        private void FaultHandlerThreadMain()
        {
            while (mFaultHandlerRunning)
            {
                mFaultHandlerEvent.WaitOne();
                if (mFaultHandlerRunning)
                {
                    try
                    {
                        ClientProxyBase.Open();
                    }
                    catch (Exception ex)
                    {
                        LOGGER.Error(string.Format("Failed to open connection. Reason: {0}", ex.Message));
                        Thread.Sleep(1000);
                    }
                }
            }
            mFaultHandlerStopEvent.Set();
        }

    }
}

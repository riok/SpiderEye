using System;
using System.Threading;
using System.Threading.Tasks;
using SpiderEye.Playground.Core.Bridge;

namespace SpiderEye.Playground.Core
{
    public class UiBridge
    {
        private readonly IUiBridgeClientService clientService;

        private static readonly Random random = new Random();
        private readonly string instanceId;

        public UiBridge(IUiBridgeClientService clientService)
        {
            this.clientService = clientService;
            instanceId = Guid.NewGuid().ToString();
        }

        public async Task RunLongProcedureOnTask()
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
        }

        public void RunLongProcedure()
        {
            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        public SomeDataModel GetSomeData()
        {
            return new SomeDataModel
            {
                Text = "Hello World",
                Number = random.Next(100),
            };
        }

        public string GetInstanceId()
        {
            return instanceId;
        }

        public Uri GetCustomFileHost()
        {
            return ProgramBase.CustomFileHost;
        }

        public double Power(PowerModel model)
        {
            return Math.Pow(model.Value, model.Power);
        }

        public void ProduceError()
        {
            throw new Exception("Intentional exception from .Net");
        }

        public void CallShowMessage()
        {
            clientService.ShowMessage("This is the message");
        }

        public async Task<string> CallPrompt()
        {
            return await clientService.Prompt("This is the prompt?");
        }
    }
}

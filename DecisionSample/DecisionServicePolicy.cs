﻿using MultiWorldTesting;
using Newtonsoft.Json;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace DecisionSample
{
    internal class DecisionServicePolicy<TContext> : IPolicy<TContext>, IDisposable
    {
        public DecisionServicePolicy(Action notifyPolicyUpdate, string modelAddress)
        {
            this.notifyPolicyUpdate = notifyPolicyUpdate;
            this.modelAddress = modelAddress;

            this.cancellationToken = new CancellationTokenSource();

            this.worker = new BackgroundWorker();
            this.worker.DoWork += PollForUpdate;
            this.worker.ProgressChanged += FoundUpdate;
            this.worker.RunWorkerAsync(this.cancellationToken);
        }

        public uint ChooseAction(TContext context)
        {
            // TODO: how to create an example from just the context?
            string exampleLine = string.Empty;

            IntPtr example = VowpalWabbitInterface.ReadExample(vw, exampleLine);
            VowpalWabbitInterface.Predict(vw, example);
            VowpalWabbitInterface.FinishExample(vw, example);

            return (uint)VowpalWabbitInterface.GetCostSensitivePrediction(example);
        }

        public void StopPolling()
        {
            this.cancellationToken.Cancel();
            this.VWFinish();
        }

        public void Dispose() { }

        void FoundUpdate(object sender, ProgressChangedEventArgs e)
        {
            var newModelFileName = e.UserState as string;
            
            this.VWFinish(); // Finish previous run before initializing on new file
            this.VWInitialize(string.Format("-t -i {0}", newModelFileName));
            
            this.notifyPolicyUpdate();
        }

        void PollForUpdate(object sender, DoWorkEventArgs e)
        {
            var cancelToken = e.Argument as CancellationTokenSource;
            while (!cancelToken.IsCancellationRequested)
            {
                System.Threading.Thread.Sleep(PollDelayInMiliseconds);

                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(modelAddress);
                    if (modelDate != null)
                    {
                        request.IfModifiedSince = modelDate.DateTime;
                    }

                    using (WebResponse response = request.GetResponse())
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        var model = JsonConvert.DeserializeObject<ModelTransferData>(sr.ReadToEnd());

                        // Write model to file
                        File.WriteAllBytes(model.Name, Convert.FromBase64String(model.ContentAsBase64));

                        // Store last modified date for conditional get
                        modelDate = model.LastModified;

                        // Notify caller of model update
                        worker.ReportProgress(0, model.Name);
                    }
                }
                catch (Exception ex)
                {
                    bool logErrors = true;
                    if (ex is WebException)
                    {
                        HttpWebResponse httpResponse = ((WebException)ex).Response as HttpWebResponse;
                        if (httpResponse.StatusCode == HttpStatusCode.NotModified)
                        {
                            // Exception is raised for NotModified http response but this is expected.
                            logErrors = false;
                        }
                    }
                    if (logErrors)
                    {
                        Trace.TraceError("Failed to retrieve new model information.");
                        Trace.TraceError(ex.ToString());
                    }
                }
            }
        }

        void VWInitialize(string arguments)
        {
            vw = VowpalWabbitInterface.Initialize(arguments);
            vwState = VowpalWabbitState.Initialized;
        }

        void VWFinish()
        {
            if (vwState == VowpalWabbitState.Initialized)
            {
                VowpalWabbitInterface.Finish(vw);
                vwState = VowpalWabbitState.Finished;
            }
        }

        IntPtr vw;
        VowpalWabbitState vwState;

        BackgroundWorker worker;
        CancellationTokenSource cancellationToken;
        
        Action notifyPolicyUpdate;
        string modelAddress;
        DateTimeOffset modelDate;

        #region Constants

        // TODO: Configurable?
        private readonly int PollDelayInMiliseconds = 5000;

        #endregion
    }

}

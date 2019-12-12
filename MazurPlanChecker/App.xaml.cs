using ESAPIX.Bootstrapper;
using MazurPlanChecker.Views;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ESAPIX.Common;
using ESAPIX.Common.Args;
using EvilDICOM.Network;
using EvilDICOM.Core.Helpers;
using System.IO;

namespace MazurPlanChecker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {

            // Store the details of the daemon (Ae Title , IP , port )
            var daemon = new Entity("VMSDBD1", "192.168.1.11", 5678);
            // Store the details of the client (Ae Title , port ) -> IP address is determined by CreateLocal() method
            var local = Entity.CreateLocal("DICOMEC1", 5681);
            // Set up a client ( DICOM SCU = Service Class User )
            var client = new DICOMSCU(local);
            // Set up a receiver to catch the files as they come in
            var receiver = new DICOMSCP(local);
            // Let the daemon know we can take anything it sends
            receiver.SupportedAbstractSyntaxes = AbstractSyntax.ALL_RADIOTHERAPY_STORAGE;
            // Set up storage location
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var storagePath = Path.Combine(desktopPath, "DICOM Storage");
            Directory.CreateDirectory(storagePath);
            // Set the action when a DICOM files comes in                        
            receiver.DIMSEService.CStoreService.CStorePayloadAction = (dcm, asc) =>
            {
                var path = Path.Combine(storagePath, dcm.GetSelector().
                SOPInstanceUID.Data + ".dcm");
                Console.WriteLine($"Writing file { path }... ");
                dcm.Write(path);
                return true; 
            };
            receiver.ListenForIncomingAssociations(true);

            // Build a finder class to help with C- FIND operations
            var finder = client.GetCFinder(daemon);            
            var studies = finder.FindStudies("US-EC-020");
            var series = finder.FindSeries(studies);
            // Filter series by modality , then create list of
            //we're not finding any studies - why?        
            var plans = series.Where(s => s.Modality == "RTPLAN").SelectMany(ser => finder.FindImages(ser));
            var doses = series.Where(s => s.Modality == "RTDOSE").SelectMany(ser => finder.FindImages(ser));
            var cts = series.Where(s => s.Modality == "CT").SelectMany(ser => finder.FindImages(ser));
            var mover = client.GetCMover(daemon);
            ushort msgId = 1;
            foreach (var plan in plans)
            {
                Console.WriteLine($"Sending plan { plan.SOPInstanceUID }... ");
                // Make sure Mobius is on the whitelist of the daemon
                var response = mover.SendCMove(plan, local.AeTitle, ref msgId);
                Console.WriteLine($"DICOM  CMove Results: ");
                Console.WriteLine($"Number of Completed Operations: { response.NumberOfCompletedOps }");
                Console.WriteLine($"Number of Failed Operations: { response.NumberOfFailedOps }");
                Console.WriteLine($"Number of Remaining Operations: { response.NumberOfRemainingOps}");
                Console.WriteLine($"Number of Warning Operations: { response.NumberOfWarningOps}");
            }
            Console.Read() ; 
            // Stop here
            
            //var myEntity = new Entity("VMSDBD1","192.168.1.11",5678);

            //var scp = new DICOMSCP(myEntity);
            //scp.ListenForIncomingAssociations(keepListenerRunning: true);

            //var en1 = Entity.CreateLocal("EvilDICOM", "666");
            //var scu = new DICOMSCU(en1);


            ////USING EVILDICOM
            ////var localEntity = new Entity("DCMTK")
            //var daemon = new Entity("VarianDaemon", "192.168.1.11", 5678);
            ////var client = new DICOMSCP();
            //var client = new DICOMSCU(Entity.CreateLocal("MyEntity", 9999));

            //var finder = client.GetCFinder(daemon);
            //var studies = finder.FindStudies("PATIENTID");
            //var series = finder.FindSeries(studies);
            ////var series = finder.FindSeries(studies).Where(s=>s.Modality=="REG");

            //var mover = client.GetCMover(daemon);
            //mover.SendCMove(series.First(),daemon.AeTitle)
            //var records = finder.FindRTRecords(series);
            //ecords.First().TreatmentDate

            //UNCOMMENT FOR OUR EXAMPLE PLANCHECKER
            //string[] args = e.Args;
            //base.OnStartup(e);
            //var bs = new AppBootstrapper<MainView>(() => { return VMS.TPS.Common.Model.API.Application.CreateApplication(); });
            ////You can use the following to load a context (for debugging purposes)
            ////args = ContextIO.ReadArgsFromFile(@"C: \Users\cwalker\Desktop\context.txt");
            ////Might disable (uncomment) for plugin mode
            //bs.IsPatientSelectionEnabled = false;
            //bs.Run(args);
        }

        protected override void OnExit(ExitEventArgs e)
{
    AppComThread.Instance.Dispose();
    base.OnExit(e);
}
    }
}

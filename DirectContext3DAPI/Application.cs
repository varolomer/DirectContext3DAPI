using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExternalService;
using Autodesk.Revit.DB.Events;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;

namespace DirectContext3DAPI
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.Attributes.Regeneration(Autodesk.Revit.Attributes.RegenerationOption.Manual)]
    public class Application : IExternalApplication
    {
        static Application s_applicationInstance;
        HashSet<Document> m_documents;

        List<RevitElementDrawingServer> m_servers;
        List<CustomServer> m_CustomServers;

        XYZ m_offset = new XYZ(0, 0, 45);


      /// <summary>
      /// Implements the OnShutdown event
      /// </summary>
      /// <param name="application"></param>
      /// <returns>Result that indicates whether the external application has completed its work successfully.</returns>
      public Result OnShutdown(UIControlledApplication application)
      {
         // remove the event.
         application.ControlledApplication.DocumentClosing -= OnDocumentClosing;
         return Result.Succeeded;
      }

        /// <summary>
        /// Implements the OnStartup event
        /// </summary>
        /// <param name="application"></param>
        /// <returns>Result that indicates whether the external application has completed its work successfully.</returns>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Register events. 
                application.ControlledApplication.DocumentClosing += new EventHandler
                    <Autodesk.Revit.DB.Events.DocumentClosingEventArgs>(OnDocumentClosing);
                m_servers = new List<RevitElementDrawingServer>();
                m_CustomServers = new List<CustomServer>();

                m_documents = new HashSet<Document>();

                s_applicationInstance = this;

            }
            catch (Exception)
            {
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Implements the OnDocumentClosing event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void OnDocumentClosing(object sender, DocumentClosingEventArgs args)
        {
            unregisterServers(args.Document, false);
            unregisterCustomServers(args.Document, false);
        }


        /// <summary>
        /// Responds to the external command CommandDuplicateGraphics.
        /// </summary>
        /// <param name="document"></param>
        public static void ProcessCommandDuplicateGraphics(Document document)
        {
            if (s_applicationInstance != null)
            {
                s_applicationInstance.AddMultipleRevitElementServers(new UIDocument(document));
            }
        }
        /// <summary>
        /// Responds to the external command CommandClearExternalGraphics.
        /// </summary>
        /// <param name="document"></param>
        public static void ProcessCommandClearExternalGraphics(Document document)
        {
            if (s_applicationInstance != null)
            {
                s_applicationInstance.unregisterServers(null, true);
            }
        }

        /// <summary>
        /// Responds to the external command ShowMeshGraphics.
        /// </summary>
        /// <param name="document"></param>
        public static void ShowMeshGraphics(Document document)
        {
            if (s_applicationInstance != null)
            {
                s_applicationInstance.AddMultipleCustomServers(new UIDocument(document));
            }
        }

        /// <summary>
        /// Responds to the external command ClearMeshGraphics.
        /// </summary>
        /// <param name="document"></param>
        public static void ClearMeshGraphics(Document document)
        {
            if (s_applicationInstance != null)
            {
                s_applicationInstance.unregisterCustomServers(null, true);
            }
        }

        private void AddMultipleRevitElementServers(UIDocument uidoc)
        {
            IList<Reference> references = uidoc.Selection.PickObjects(ObjectType.Element, "Select elements to duplicate with DirectContext3D");

            ExternalService directContext3DService = ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.DirectContext3DService);
            MultiServerService msDirectContext3DService = directContext3DService as MultiServerService;
            IList<Guid> serverIds = msDirectContext3DService.GetActiveServerIds();

            // Create one server per element.
            foreach (Reference reference in references)
            {
                Element elem = uidoc.Document.GetElement(reference);

                RevitElementDrawingServer revitServer = new RevitElementDrawingServer(uidoc, elem, m_offset);
                directContext3DService.AddServer(revitServer);
                m_servers.Add(revitServer);

                serverIds.Add(revitServer.GetServerId());
            }

            msDirectContext3DService.SetActiveServers(serverIds);

            m_documents.Add(uidoc.Document);
            uidoc.UpdateAllOpenViews();
        }

        private void AddMultipleCustomServers(UIDocument uidoc)
        {
            ExternalService directContext3DService = ExternalServiceRegistry.GetService(ExternalServices.BuiltInExternalServices.DirectContext3DService);
            MultiServerService msDirectContext3DService = directContext3DService as MultiServerService;
            IList<Guid> serverIds = msDirectContext3DService.GetActiveServerIds();


            CustomServer revitServer = new CustomServer(uidoc);
            directContext3DService.AddServer(revitServer);
            m_CustomServers.Add(revitServer);

            serverIds.Add(revitServer.GetServerId());

            msDirectContext3DService.SetActiveServers(serverIds);

            m_documents.Add(uidoc.Document);
            uidoc.UpdateAllOpenViews();
        }



        private void unregisterServers(Document document, bool updateViews)
        {
            ExternalServiceId externalDrawerServiceId = ExternalServices.BuiltInExternalServices.DirectContext3DService;
            var externalDrawerService = ExternalServiceRegistry.GetService(externalDrawerServiceId) as MultiServerService;
            if (externalDrawerService == null)
                return;

            foreach (var registeredServerId in externalDrawerService.GetRegisteredServerIds())
            {
                var externalDrawServer = externalDrawerService.GetServer(registeredServerId) as RevitElementDrawingServer;
                if (externalDrawServer == null)
                    continue;
                if (document != null && !document.Equals(externalDrawServer.Document))
                    continue;
                externalDrawerService.RemoveServer(registeredServerId);
            }

            if (document != null)
            {
                m_servers.RemoveAll(server => document.Equals(server.Document));

                if (updateViews)
                {
                    UIDocument uidoc = new UIDocument(document);
                    uidoc.UpdateAllOpenViews();
                }

                m_documents.Remove(document);
            }
            else
            {
                m_servers.Clear();

                if (updateViews)
                    foreach (var doc in m_documents)
                    {
                        UIDocument uidoc = new UIDocument(doc);
                        uidoc.UpdateAllOpenViews();
                    }

                m_documents.Clear();
            }
        }

        private void unregisterCustomServers(Document document, bool updateViews)
        {
            ExternalServiceId externalDrawerServiceId = ExternalServices.BuiltInExternalServices.DirectContext3DService;
            var externalDrawerService = ExternalServiceRegistry.GetService(externalDrawerServiceId) as MultiServerService;
            if (externalDrawerService == null)
                return;

            foreach (var registeredServerId in externalDrawerService.GetRegisteredServerIds())
            {
                var externalDrawServer = externalDrawerService.GetServer(registeredServerId) as CustomServer;
                if (externalDrawServer == null)
                    continue;
                if (document != null && !document.Equals(externalDrawServer.Document))
                    continue;
                externalDrawerService.RemoveServer(registeredServerId);
            }

            if (document != null)
            {
                m_servers.RemoveAll(server => document.Equals(server.Document));

                if (updateViews)
                {
                    UIDocument uidoc = new UIDocument(document);
                    uidoc.UpdateAllOpenViews();
                }

                m_documents.Remove(document);
            }
            else
            {
                m_servers.Clear();

                if (updateViews)
                    foreach (var doc in m_documents)
                    {
                        UIDocument uidoc = new UIDocument(doc);
                        uidoc.UpdateAllOpenViews();
                    }

                m_documents.Clear();
            }
        }

    }
}

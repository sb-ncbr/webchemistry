﻿using System;
using System.Windows;
using Microsoft.Practices.ServiceLocation;
using Ninject;
using WebChemistry.SiteBinder.Silverlight.DataModel;
using WebChemistry.SiteBinder.Silverlight.ViewModel;
using WebChemistry.Silverlight.Common.Services;
using WebChemistry.Silverlight.Common;
using WebChemistry.Framework.Core;
using System.Windows.Markup;

namespace WebChemistry.SiteBinder.Silverlight
{
public partial class App : Application
    {

        public App()
        {
            PortableTPL.TaskScheduler.ProcessorCount = Math.Max(Environment.ProcessorCount / 2, 1);

            this.Startup += this.Application_Startup;
            this.Exit += this.Application_Exit;
            this.UnhandledException += this.Application_UnhandledException;

            InitializeComponent();
        }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            WebChemistry.Silverlight.Common.Utils.CultureHelper.SetDefaultCultureToEnUS();

            var kernel = new Ninject.StandardKernel();
            var locator = new NinjectCommonLocator(kernel);

            ServiceLocator.SetLocatorProvider(() => locator);

            kernel.Load<ServicesModule>();

            kernel.Bind<Session>().To<Session>().InSingletonScope();
            kernel.Bind<MainViewModel>().To<MainViewModel>().InSingletonScope();
            kernel.Bind<InputViewModel>().To<InputViewModel>().InSingletonScope();
            kernel.Bind<ResultViewModel>().To<ResultViewModel>().InSingletonScope();
            kernel.Bind<MainPage>().To<MainPage>().InSingletonScope();
            
            var mp = kernel.Get<MainPage>();
            mp.Language = XmlLanguage.GetLanguage(System.Threading.Thread.CurrentThread.CurrentCulture.Name);
            this.RootVisual = mp;
            
            LogService.Default.Message("Welcome to SiteBinder " + SiteBinder.Core.SiteBinderVersion.Version + "!");
        }

        private void Application_Exit(object sender, EventArgs e)
        {

        }

        private void Application_UnhandledException(object sender, ApplicationUnhandledExceptionEventArgs e)
        {
            // If the app is running outside of the debugger then report the exception using
            // the browser's exception mechanism. On IE this will display it a yellow alert 
            // icon in the status bar and Firefox will display a script error.
            if (!System.Diagnostics.Debugger.IsAttached)
            {

                // NOTE: This will allow the application to continue running after an exception has been thrown
                // but not handled. 
                // For production applications this error handling should be replaced with something that will 
                // report the error to the website and stop the application.
                e.Handled = true;

                
                LogService.Default.Error("General Error", e.ExceptionObject.Message);

                Deployment.Current.Dispatcher.BeginInvoke(delegate { ReportErrorToDOM(e); });
            }
        }

        private void ReportErrorToDOM(ApplicationUnhandledExceptionEventArgs e)
        {
            try
            {
                string errorMsg = e.ExceptionObject.Message + e.ExceptionObject.StackTrace;
                errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");

                System.Windows.Browser.HtmlPage.Window.Eval("throw new Error(\"Unhandled Error in Silverlight Application " + errorMsg + "\");");
            }
            catch (Exception)
            {
            }
        }
    }
}

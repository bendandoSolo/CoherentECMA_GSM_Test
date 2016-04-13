using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using ECMA_GSM_Test.ViewModels;

using Coherent.MvvmHelpers;


namespace ECMA_GSM_Test
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        MainViewModel mainViewModel;

        protected override void OnStartup(StartupEventArgs e)
        {
            mainViewModel = new MainViewModel();

            //we add commands here...
            mainViewModel.StartCommand = new DelegateCommand<object>(mainViewModel.StartService);
            mainViewModel.StopCommand = new DelegateCommand<object>(mainViewModel.StopService);
            mainViewModel.RefreshSerialPortsCommand = new DelegateCommand<object>(mainViewModel.RefreshSerialPorts);
            mainViewModel.InitialiseSerialPorts(null);

            MainWindow mainWindow = new MainWindow();
            mainWindow.MainView.DataContext = mainViewModel;

            mainWindow.Show();

        }


    }
}

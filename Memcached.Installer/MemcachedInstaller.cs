using System;
using System.Collections;
using System.ComponentModel;
using System.ServiceProcess;
using System.IO;

using Microsoft.Win32;

namespace Memcached.Installer
{
  [RunInstaller(true)]
  public class MemcachedInstaller : System.Configuration.Install.Installer
  {
    readonly ServiceInstaller _serviceInstaller = new ServiceInstaller();
    readonly ServiceProcessInstaller _serviceProcessInstaller = new ServiceProcessInstaller();
    readonly string _directoryName;
    readonly string _executable;

    public MemcachedInstaller()
    {
      var pathForSelf = typeof(MemcachedInstaller).Assembly.Location;
      _directoryName = Path.GetFileName(Path.GetDirectoryName(pathForSelf));
      _executable = Path.Combine(Path.GetDirectoryName(pathForSelf), "Memcached.exe");
      ConfigureServiceInstaller(_serviceInstaller);
      ConfigureServiceProcessInstaller(_serviceProcessInstaller);
      Installers.AddRange(new System.Configuration.Install.Installer[] { _serviceProcessInstaller, _serviceInstaller });
    }

    public void ConfigureServiceInstaller(ServiceInstaller installer)
    {
      installer.ServiceName = "Memcached." + _directoryName;
      installer.Description = "Memcached." + _directoryName;
      installer.DisplayName = "Memcached";
      installer.StartType = ServiceStartMode.Automatic;
    }

    public void ConfigureServiceProcessInstaller(ServiceProcessInstaller installer)
    {
      installer.Account = ServiceAccount.NetworkService;
    }

    public override void Install(IDictionary stateSaver)
    {
      base.Install(stateSaver);
      using (var system = Registry.LocalMachine.OpenSubKey("System"))
      {
        if (system == null) throw new InvalidOperationException("Unable to open registry sub-key System");
        using (var currentControlSet = system.OpenSubKey("CurrentControlSet"))
        {
          if (currentControlSet == null) throw new InvalidOperationException("Unable to open registry sub-key CurrentControlSet");
          using (var services = currentControlSet.OpenSubKey("Services"))
          {
            if (services == null) throw new InvalidOperationException("Unable to open registry sub-key Services");
            using (var service = services.OpenSubKey(_serviceInstaller.ServiceName, true))
            {
              if (service == null) throw new InvalidOperationException("Unable to open registry sub-key for service");
              service.SetValue("Description", _serviceInstaller.Description);
              var image = "\"" + _executable + "\"" + " -d runservice -m 256";
              service.SetValue("ImagePath", image);
              service.Close();
              services.Close();
              currentControlSet.Close();
              system.Close();
            }
          }
        }
      }
    }
  }
}
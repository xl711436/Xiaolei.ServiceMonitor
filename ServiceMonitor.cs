using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xiaolei.ServiceLib;
using Xiaolei.TraceLib;

namespace Xiaolei.ServiceMonitor
{
    /// <summary> 实现类似windows 服务的编程模型
    /// </summary>
    public class ServiceMonitor
    {


        /// <summary>进行监控的服务名称列表
        /// </summary>
        private List<string> moniterServiceList ;

        /// <summary>监控服务的timer
        /// </summary>
        private System.Timers.Timer timer_check;

        /// <summary>重启服务的timer
        /// </summary>
        private System.Timers.Timer timer_restart;

        /// <summary>重启服务的名称和时间的字典
        /// </summary>
        private Dictionary<string, int> restartServiceDic;

    
        private DateTime lastDate;
          

        /// <summary>服务是否结束的标记
        /// </summary>
        public bool ExitFlag { get; set; }

        public ServiceMonitor()
        {
            TraceHelper.TraceInfo("ServiceMonitor Instance");

            try
            {
                int monitorIntervalSecond = Convert.ToInt32(ConfigurationManager.AppSettings["MonitorIntervalSecond"]);
                string monitorServiceName = ConfigurationManager.AppSettings["MonitorServiceName"];



                timer_check = new System.Timers.Timer(1000 * monitorIntervalSecond);
                timer_check.Enabled = true;
                timer_check.Elapsed += Timer_check_Elapsed;

                timer_restart = new System.Timers.Timer(1000 * 60);
                timer_restart.Enabled = true;
                timer_restart.Elapsed += Timer_restart_Elapsed;

                moniterServiceList = monitorServiceName.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();

                restartServiceDic = GetRestartDic();

                lastDate = DateTime.Now.Date;
            }
            catch (Exception Exc)
            {
                TraceHelper.TraceInfo("构造异常" + Exc.Message + Exc.StackTrace);
            }
        }

        private Dictionary<string, int> GetRestartDic()
        {
            Dictionary<string, int> R_Resullt = new Dictionary<string, int>();

            string restartService = ConfigurationManager.AppSettings["RestartService"];

            string[] tempArray = restartService.Split(new char[] { ':' }, StringSplitOptions.RemoveEmptyEntries);


            foreach (string curService in tempArray)
            {
                string[] tempConfig = curService.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                if (tempConfig.Length == 2)
                {
                    if (!R_Resullt.ContainsKey(tempConfig[0]))
                    {
                        R_Resullt.Add(tempConfig[0], Convert.ToInt32(tempConfig[1]));
                    }
                }
            }

            return R_Resullt;
        }

        public void Start()
        {
            TraceHelper.TraceInfo("ServiceMonitor Start");
            timer_check.Start();
            timer_restart.Start();
            TraceHelper.TraceInfo("ServiceMonitor Start ok");
        }

        public void Stop()
        {
            TraceHelper.TraceInfo("ServiceMonitor Stop");

            try
            {
                timer_check.Stop();
                timer_check.Dispose();

                timer_restart.Stop();
                timer_restart.Dispose();

            }
            catch(Exception Exc)
            {
                TraceHelper.TraceInfo("ServiceMonitor Stop 异常" + Exc.Message + Exc.StackTrace);
            }
            TraceHelper.TraceInfo("ServiceMonitor Stop ok");
        }


        private void Timer_check_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            TraceHelper.TraceInfo("ServiceMonitor Check start");

            foreach(string curServiceName in moniterServiceList)
            {
                try
                {
                    if (!ServiceHelper.IsRunning(curServiceName))
                    {
                        TraceHelper.TraceInfo(curServiceName + " not running ");

                        if (ServiceHelper.StartService(curServiceName))
                        {
                            TraceHelper.TraceInfo(curServiceName + " started   ");
                        }
                        else
                        {
                            TraceHelper.TraceInfo(curServiceName + " start fail   ");
                        }
                    }
                }
                catch(Exception Exc)
                {
                    TraceHelper.TraceInfo("check service " + curServiceName + " exception  " + Exc.Message + Exc.StackTrace);
                }
            }

            TraceHelper.TraceInfo("ServiceMonitor Check over");
        }


        private void Timer_restart_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {

            TraceHelper.TraceInfo("ServiceMonitor restart start");

            if (lastDate != DateTime.Now.Date)
            {
                lastDate = DateTime.Now.Date;
                restartServiceDic = GetRestartDic();
                TraceHelper.TraceInfo("更新 lastDate" + lastDate.ToShortDateString());

            }

            List<string> removeServiceList = new List<string>();

            foreach (KeyValuePair<string, int> curItem in restartServiceDic)
            {
                try
                {
                    if (curItem.Value == DateTime.Now.Hour)
                    {
                        removeServiceList.Add(curItem.Key);

                        TraceHelper.TraceInfo("停止服务");
                        ServiceHelper.StopService(curItem.Key);
                        TraceHelper.TraceInfo("启动服务");
                        ServiceHelper.StartService(curItem.Key);
                        TraceHelper.TraceInfo("启动服务 ok");
                    }
                }
                catch (Exception Exc)
                {
                    TraceHelper.TraceInfo("重启服务的 timer 异常： " + Exc.Message + Exc.StackTrace);
                }
            }

            foreach (string curName in removeServiceList)
            {
                if (restartServiceDic.ContainsKey(curName))
                {
                    restartServiceDic.Remove(curName);
                }
            }

            TraceHelper.TraceInfo("ServiceMonitor restart over");
        }

    }

}

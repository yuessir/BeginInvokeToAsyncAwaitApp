using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private DispatcherWaiter _waiter;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Print("Application Dispatcher");
            PrintLine();
            _waiter = new DispatcherWaiter(this);
            StartAsync().ConfigureAwait(true);
        }


        private async Task StartAsync()
        {
            //Print("Call DoSomethingAsync() on main GUI");
            //await DoSomethingAsync();

            //PrintLine();
            //await DoSomethingFirstAsync();

            PrintLine();
            Print("DoSomething() callback in OnCompleted() on Main GUI");
            await DoSomethingCallback();

            //PrintLine();
            //await Task.Run(() =>
            //{
            //    Print("DoSomething() callback in OnCompleted() on background");
            //    return DoSomethingCallback();
            //});

            //PrintLine();
            //Print("DoSomethingMultipleTimesWithCancellation");
            //await DoSomethingMultipleTimesWithCancellationAsync();
        }

        public delegate void AddMsgDelegate(string msg);

        private void Print(string msg)
        {
            if (Thread.CurrentThread.Name == null)
            {
                if (Thread.CurrentThread.IsThreadPoolThread)
                {
                    Thread.CurrentThread.Name = "來自池的緒";
                }
                else
                {
                    Thread.CurrentThread.Name = "一般緒";
                }
            }

            var param = msg + ", " + Thread.CurrentThread.Name + ", threadId: " +
                        Thread.CurrentThread.ManagedThreadId.ToString();
            BeginInvoke(new AddMsgDelegate(AddMsg), param);
        }

        private void PrintLine()
        {
            BeginInvoke(new AddMsgDelegate(AddMsg), "");
        }

        private void AddMsg(string msg)
        {
            listBox1.Items.Add(msg);
        }

        private async Task DoSomethingAsync()
        {
            Print("Before WaitAsync");

            //  await _waiter.WaitAsync();
            var status = await _waiter.WaitAsync();
            Print("Task status: " + status.ToString());
            Print("After WaitAsync");
            DoSomething();
        }

        private Task DoSomethingFirstAsync()
        {
            return Task.Run(() =>
            {
                Print("Call DoSomethingAsync() on background");
                return DoSomethingAsync();
            });
        }
        private Task DoSomethingUpdateAsync()
        {
            return Task.Run(() =>
           {
               Print("Call DoSomethingUpdateAsync() on background");
               _waiter.WaitAsync();//執行後回到主緒
               label1.Text = "updating safe....";
           });
        }

        private Task DoSomethingCallback()
        {
            Print("Before OnCompleted");

            var tcs = new TaskCompletionSource<int>();
            //OnCompleted 對外開放接口
            _waiter.OnCompleted(async () =>
              {
                  Print("After OnCompleted");
                  // DoSomething();
                  //下行執行不會錯誤,拿除_waiter.WaitAsync(); 噴跨緒錯誤
                  await DoSomethingUpdateAsync();//假設這是外部傳入的action 並不帶有 _waiter.WaitAsync();
                  //
                  tcs.TrySetResult(1);
              });

            return tcs.Task;
        }

        private async Task DoSomethingMultipleTimesWithCancellationAsync()
        {
            Print("Before OnCompleted");

            var tasks = new List<Task>();
            for (var i = 0; i < 5; i++)
            {
                var task = CreateTask(i);
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        private Task CreateTask(int i)
        {
            return Task.Run(async () =>
            {
                Print("Create Task " + i.ToString());
                var status = await _waiter.WaitAsync();
                Print("Task " + i.ToString() + ": " + status.ToString());
                PrintLine();
                Print("Task " + i.ToString() + " completed");

            });
        }

        private void DoSomething()
        {
            label1.Text = "updating...";
        }

    }
}

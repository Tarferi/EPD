using System;
using System.ComponentModel;
using System.Windows;

namespace StarcraftEPDTriggers.src.data {
    class AsyncWorker {
        private Func<object, object> caller;
        private Action<object> finish;

        public AsyncWorker(object paramss, Func<object, object> caller, Action<object> finish) {
            BackgroundWorker bw = new BackgroundWorker();
            this.caller = caller;
            this.finish = finish;
            bw.DoWork += worker_do;
            bw.RunWorkerCompleted += worker_finished;
            bw.RunWorkerAsync(paramss);
        }

        private void worker_do(object sender, DoWorkEventArgs e) {
#if DEBUG
            e.Result = caller(e.Argument);
#else
            try {
                e.Result = caller(e.Argument);
            } catch (NotImplementedException exc) {
                Application.Current.Dispatcher.Invoke(() => {
                    new WndError(exc).ShowDialog();
                });
        }
#endif
        }

        private void worker_finished(object sender, RunWorkerCompletedEventArgs e) {
            finish(e.Result);
        }

    }
}

namespace SmallBasicLive
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Windows.Input;
    using Microsoft.SmallBasic;
    using Microsoft.SmallBasic.Library;
    using Microsoft.SmallBasic.Library.Internal;

    public class LivePreviewRunner : MarshalByRefObject, ISupportInitialize, IDisposable, ICommand
    {
        static LivePreviewRunner instance;

        public LivePreviewRunner()
        {
            instance = this;
        }

        public void Run(string program)
        {
        }

        public void Dispose()
        {
            TextWindow.PauseIfVisible();
            SmallBasicApplication.EndProgram();
        }

        public void BeginInit()
        {
            SmallBasicApplication.BeginProgram();
        }

        public void EndInit()
        {
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return parameter is string && !string.IsNullOrEmpty((string)parameter);
        }

        public void Execute(object parameter)
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                var sb = (string)state;
                var compiler = new Compiler();
                compiler.Initialize();

                var errors = compiler.Compile(new StringReader(sb));
                var asm = new CodeGenerator(compiler.Parser, compiler.TypeInfoBag).GenerateAssembly();

                if (asm != null)
                {
                    var program = asm.GetType("Program");
                    if (program != null)
                    {
                        try
                        {
                            program.InvokeMember("Run", BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod, null, null, null);
                        }
                        catch { }
                    }
                }
            }, parameter);
        }
    }
}

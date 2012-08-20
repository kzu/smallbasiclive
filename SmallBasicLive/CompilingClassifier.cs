namespace SmallBasicLive
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.ComponentModel.Composition;
    using System.IO;
    using System.Windows.Input;
    using AutoMapper;
    using Microsoft.Nautilus.Text;
    using Microsoft.Nautilus.Text.Classification;

    public sealed class CompilingClassifier : IDisposable
    {
        [ContentType("text.smallbasic")]
        [Export(typeof(ClassifierProvider))]
        public IClassifier CreateClassifier(ITextBuffer textBuffer)
        {
            return new Classifier(textBuffer);
        }

        class Classifier : IClassifier, IDisposable
        {
            private ITextBuffer textBuffer;
            // TODO: implement some kind of recycling.
            private AppDomain appDomain;

            public event EventHandler<ClassificationChangedEventArgs> ClassificationChanged;

            public Classifier(ITextBuffer textBuffer)
            {
                this.textBuffer = textBuffer;
                this.textBuffer.Changed += (sender, args) => RunProgram();
                SetupAppDomain();
            }

            private void SetupAppDomain()
            {
                var setup = Mapper.Map(AppDomain.CurrentDomain.SetupInformation, new AppDomainSetup());
                setup.PrivateBinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings");

                appDomain = AppDomain.CreateDomain("Debuggee", AppDomain.CurrentDomain.Evidence, setup);

                appDomain.AssemblyResolve += (sender, args) =>
                    {
                        return AppDomain.CurrentDomain.Load(args.Name);
                    };

                var runner = (ISupportInitialize)appDomain.CreateInstanceAndUnwrap("SmallBasicLive", "SmallBasicLive.LivePreviewRunner");
                runner.BeginInit();
                runner.EndInit();
            }

            private void RunProgram()
            {
                var text = this.textBuffer.CurrentSnapshot.GetText(0, this.textBuffer.CurrentSnapshot.Length);

                Dispose();
                SetupAppDomain();

                var runner = (ICommand)appDomain.CreateInstanceAndUnwrap("SmallBasicLive", "SmallBasicLive.LivePreviewRunner");
                if (runner.CanExecute(text))
                    runner.Execute(text);
            }

            public IList<ClassificationSpan> GetClassificationSpans(SnapshotSpan textSpan)
            {
                return new List<ClassificationSpan>();
            }

            public void Dispose()
            {
                var runner = (IDisposable)appDomain.CreateInstanceAndUnwrap("SmallBasicLive", "SmallBasicLive.LivePreviewRunner");
                runner.Dispose();
                //AppDomain.Unload(appDomain);
            }
        }

        public void Dispose()
        {
        }
    }
}

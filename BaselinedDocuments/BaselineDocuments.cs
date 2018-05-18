using System;

namespace BaselinedDocuments
{
    class BaselineDocuments
    {
        static void Main()
        {
            IOutputWriter logger = new OutputWriter();

            try
            {
                AppData data = new AppData();
                Engine engine = new Engine(logger, data);
                engine.ProcessZips();
            }
            catch (Exception ex)
            {
                logger.LogMessage(ex.Message);
            }
        }
    }
}


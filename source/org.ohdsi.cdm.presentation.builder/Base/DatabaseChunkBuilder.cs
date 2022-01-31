using org.ohdsi.cdm.framework.common.Base;
using org.ohdsi.cdm.framework.common.Definitions;
using org.ohdsi.cdm.framework.desktop.Base;
using org.ohdsi.cdm.framework.desktop.Databases;
using org.ohdsi.cdm.framework.desktop.Enums;
using System;
using System.Collections.Generic;
using System.Data.Odbc;
using System.Diagnostics;
using org.ohdsi.cdm.framework.desktop.Helpers;

namespace org.ohdsi.cdm.presentation.builder.Base
{
    public class DatabaseChunkBuilder
    {
        #region Variables

        private readonly int _chunkId;
        private readonly Func<IPersonBuilder> _createPersonBuilder;
        #endregion

        #region Constructors

        public DatabaseChunkBuilder(int chunkId, Func<IPersonBuilder> createPersonBuilder)
        {
            _chunkId = chunkId;
            _createPersonBuilder = createPersonBuilder;
        }
        #endregion

        #region Methods
        public DatabaseChunkPart Process(IDatabaseEngine sourceEngine, string sourceSchemaName, List<QueryDefinition> sourceQueryDefinitions, OdbcConnection sourceConnection, string vendor)
        {
            try
            {
                Console.WriteLine("DatabaseChunkBuilder");

                var part = new DatabaseChunkPart(_chunkId, _createPersonBuilder, "0", 0);
                part.FileLog = (string c) => FileLogger.WriteLog(c);

                var timer = new Stopwatch();
                timer.Start();
                
                FileLogger.WriteLog($"[CHUNK#{_chunkId}] Query Loader Start");

                var result = part.Load(sourceEngine, sourceSchemaName, sourceQueryDefinitions, sourceConnection, vendor);
                if (result.Value != null)
                {
                    Logger.Write(_chunkId, LogMessageTypes.Info, result.Key);
                    throw result.Value;
                }

                Logger.Write(_chunkId, LogMessageTypes.Info,
                    $"ChunkId={_chunkId} was loaded - {timer.ElapsedMilliseconds} ms | {GC.GetTotalMemory(false) / 1024f / 1024f} Mb");

                Logger.WriteInfo(_chunkId, $"Chunk#{_chunkId} Start Build...");
                part.Build();
                Logger.WriteInfo(_chunkId, $"Chunk#{_chunkId} End Build");

                return part;
            }
            catch (Exception e)
            {
                Logger.WriteError(e);
                Logger.WriteError(_chunkId, e);
                throw;
            }
        }
        #endregion
    }
}

﻿using System;
using System.Linq;
using Akka.Actor;
using Couchbase;

namespace Akka.Persistence.CouchBase
{
    /// <summary>
    /// An actor extension initializing support for CouchBaseDB persistence layer.
    /// </summary>
    class CouchBaseDBExtension: IExtension
    {

        public CouchBaseJournalSettings JournalSettings { get; private set; }

        public Couchbase.Core.ICluster JournalCBCluster{get; private set;}

        public Couchbase.Core.IBucket JournalCBBucket{ get; private set;}


        public CouchbaseSnapshotSettings SnapShotStoreSettings { get; private set; }

        public Couchbase.Core.ICluster SnapShotStoreCBCluster { get; private set; }

        public Couchbase.Core.IBucket SnapShotStoreCBBucket { get; private set; }


        public CouchBaseDBExtension(ExtendedActorSystem system)
        {

            if (system == null)
                throw new ArgumentNullException("system");


            // Initialize fallback configuration defaults
            system.Settings.InjectTopLevelFallback(CouchBaseDBPersistence.DefaultConfiguration());

            var HOCON_CB_JournalConfig = system.Settings.Config.GetConfig("akka.persistence.journal.couchbase");

            JournalSettings = new CouchBaseJournalSettings(HOCON_CB_JournalConfig);

            if(JournalSettings.UseClusterHelper)
            {
                JournalCBBucket = ClusterHelper.GetBucket(JournalSettings.BucketName);
                JournalCBCluster = JournalCBBucket.Cluster;
            }
            else
            {
                // Instantiate the connection to the cluster
                JournalCBCluster = new Cluster(JournalSettings.CBClientConfiguration);

                //Open the bucket and make a reference to the CB Client Configuration
                JournalCBBucket = (CouchbaseBucket)JournalCBCluster.OpenBucket(JournalSettings.BucketName);
            }

            // Throw an exception if we reach this point without a CB Cluster, CB Config, or Bucket
            if (JournalCBCluster == null)
                throw new Exception("CouchBase Journal Cluster could not initialized.");

            // Had to do it this way since by default CB always initializes the default bucket.
            if (JournalCBBucket.Name!=JournalSettings.BucketName)
            {
                throw new Exception("CouchBase Journal bucket could not initialized.");
            }


            // Add Journal Indexes
            // Add here to create Global Secondary Indexes that cover (See covering Indexes in Couchbase website) to improve performance
            // First check if the index exists
            // SELECT * FROM system:indexes WHERE name = 'idxDocumentType_PersistenceId_SequenceNr'
            // SELECT * FROM system:indexes WHERE name = 'idxDocumentType_PersistenceId'
            // Create using these:
            // CREATE INDEX idxDocumentType_PersistenceId_SequenceNr on `SSA` (PersistenceId,SequenceNr,DocumentType) USING GSI
            // CREATE INDEX idxDocumentType_PersistenceId on `SSA` (DocumentType,PersistenceId) USING GSI

            //string N1QLQueryString = "SELECT * FROM system:indexes WHERE name = 'idxDocumentType_PersistenceId_SequenceNr'";
            //var result = JournalCBBucket.Query<dynamic>(N1QLQueryString);
            //if (result.Rows.Count == 0 && result.Success == true)
            //{
            //    N1QLQueryString = "CREATE INDEX idxDocumentType_PersistenceId_SequenceNr on `" + JournalCBBucket.Name + "` (PersistenceId,SequenceNr,DocumentType) USING GSI";
            //    result = JournalCBBucket.Query<dynamic>(N1QLQueryString);
            //    //if (result.Success != true)
            //    //    Debug.Write("Could not create index:idxDocumentType_PersistenceId_SequenceNr");
            //}
            //N1QLQueryString = "SELECT * FROM system:indexes WHERE name = 'idxDocumentType_PersistenceId";
            //result = JournalCBBucket.Query<dynamic>(N1QLQueryString);
            //if (result.Rows.Count == 0 && result.Success == true)
            //{
            //    N1QLQueryString = "CREATE INDEX idxDocumentType_PersistenceId on `" + JournalCBBucket.Name + "` (PersistenceId,DocumentType) USING GSI";
            //    result = JournalCBBucket.Query<dynamic>(N1QLQueryString);
            //    //if (result.Success != true)
            //    //    Debug.Write("Could not create index:idxDocumentType_PersistenceId");
            //}



            var HOCON_CB_SnapshotConfig = system.Settings.Config.GetConfig("akka.persistence.snapshot-store.couchbase");
            SnapShotStoreSettings = new CouchbaseSnapshotSettings(HOCON_CB_SnapshotConfig);

            if(SnapShotStoreSettings.UseClusterHelper)
            {
                SnapShotStoreCBBucket = ClusterHelper.GetBucket(SnapShotStoreSettings.BucketName);
                SnapShotStoreCBCluster = SnapShotStoreCBBucket.Cluster;
            }
            else
            {
                // Are we using the same cluster as the journal?
                if (SnapShotStoreSettings.CBClientConfiguration.Servers.All(JournalSettings.CBClientConfiguration.Servers.Contains))
                {
                    SnapShotStoreCBCluster = JournalCBCluster;

                    // Since we are using the same cluster are we using the same bucket?
                    if (SnapShotStoreSettings.BucketName == JournalSettings.BucketName)
                    {
                        SnapShotStoreCBBucket = JournalCBBucket;
                    }

                }
                else // Instantiate the connection to the new cluster
                {
                    SnapShotStoreCBCluster = new Cluster(SnapShotStoreSettings.CBClientConfiguration);

                    //Open the bucket and make a reference to the CB Client Configuration
                    SnapShotStoreCBBucket = (CouchbaseBucket)JournalCBCluster.OpenBucket(SnapShotStoreSettings.BucketName);
                }
            }

            // Throw an exception if we reach this point without a CB Cluster, CB Config, or Bucket
            if (SnapShotStoreCBCluster == null)
                throw new Exception("CouchBase Snapshot Store Cluster could not initialized.");

            if (SnapShotStoreCBBucket == null)
            {
                throw new Exception("CouchBase Snapshot Store bucket could not initialized.");
            }

            // Add Snapshot indexes
            // Add here to create Global Secondary Indexes that cover (See covering Indexes in Couchbase website) to improve performance
            // First check if the index exists
            // SELECT * FROM system:indexes WHERE name = 'idxDocumentType_PersistenceId_SequenceNr'
            // SELECT * FROM system:indexes WHERE name = 'idxDocumentType_PersistenceId_Timestamp'
            // SELECT * FROM system:indexes WHERE name = 'idxDocumentType_PersistenceId'
            // Create it if it does not
            // CREATE INDEX idxDocumentType_PersistenceId_SequenceNr on `SSA` (PersistenceId,SequenceNr,DocumentType) USING GSI
            // CREATE INDEX idxDocumentType_PersistenceId_Timestamp on `SSA` (PersistenceId,Timestamp,DocumentType) USING GSI
            // CREATE INDEX idxDocumentType_PersistenceId on `SSA` (DocumentType,PersistenceId) USING GSI

            //N1QLQueryString = "SELECT * FROM system:indexes WHERE name = 'idxDocumentType_PersistenceId_SequenceNr'";
            //result = SnapShotStoreCBBucket.Query<dynamic>(N1QLQueryString);
            //if (result.Rows.Count == 0 && result.Success == true)
            //{
            //    N1QLQueryString = "CREATE INDEX idxDocumentType_PersistenceId_SequenceNr on `" + SnapShotStoreCBBucket.Name + "` (PersistenceId,SequenceNr,DocumentType) USING GSI";
            //    result = SnapShotStoreCBBucket.Query<dynamic>(N1QLQueryString);
            //    //if (result.Success != true)
            //    //    Debug.Write("Could not create index:idxDocumentType_PersistenceId_SequenceNr");
            //}
            //N1QLQueryString = "SELECT * FROM system:indexes WHERE name = 'idxDocumentType_PersistenceId_Timestamp";
            //result = SnapShotStoreCBBucket.Query<dynamic>(N1QLQueryString);
            //if (result.Rows.Count == 0 && result.Success == true)
            //{
            //    N1QLQueryString = "CREATE INDEX idxDocumentType_PersistenceId_Timestamp on `" + SnapShotStoreCBBucket.Name + "` (PersistenceId,Timestamp,DocumentType) USING GSI";
            //    result = SnapShotStoreCBBucket.Query<dynamic>(N1QLQueryString);
            //    //if (result.Success != true)
            //    //    Debug.Write("Could not create index:idxDocumentType_PersistenceId_Timestamp");
            //}
            //N1QLQueryString = "SELECT * FROM system:indexes WHERE name = 'idxDocumentType_PersistenceId_Timestamp";
            //result = SnapShotStoreCBBucket.Query<dynamic>(N1QLQueryString);
            //if (result.Rows.Count == 0 && result.Success == true)
            //{
            //    N1QLQueryString = "CREATE INDEX idxDocumentType_PersistenceId on `" + SnapShotStoreCBBucket.Name + "` (DocumentType,PersistenceId) USING GSI";
            //    result = SnapShotStoreCBBucket.Query<dynamic>(N1QLQueryString);
            //    //if (result.Success != true)
            //    //    Debug.Write("Could not create index:idxDocumentType_PersistenceId");
            //}
        }





    }
}

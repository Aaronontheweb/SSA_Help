namespace TestCouchBaseDB
{
    /// <summary>
    /// Class used for storing intermediate result of the <see cref="IPersistentRepresentation"/>
    /// as POCO
    /// </summary>
    public class JournalEntry 
    {
        public JournalEntry()
        {
            DocumentType = "JournalEntry";
        }
        public string Id { get; set; }

        //[JsonProperty("PersistenceId")] 
        public string PersistenceId { get; set; }

        //[JsonProperty("SequenceNr")] 
        public long SequenceNr { get; set; }

        public bool isDeleted { get; set; }

        //[JsonProperty("Payload")]
        public object Payload { get; set; }

        //[JsonProperty("Manifest")] 
        public string Manifest { get; set; }

        //[JsonProperty("DocumentType")]
        public string DocumentType { get; set; }
    }
}

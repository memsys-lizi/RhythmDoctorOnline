using System.Collections.Generic;
using Newtonsoft.Json;

namespace RhythmCafe.Level
{
    public class LevelSearchResponse
    {
        public int found;
        
        public List<LevelHit> hits;
        
        public int page;
        
        public int out_of;
    }

    public class LevelHit
    {
        public LevelDocument document;
    }
    
    public class LevelDocument
    {
        public string id;
        public string song;
        public string artist;
        public List<string> authors;
        public int difficulty;
        public string image;
        public float min_bpm;
        public float max_bpm;
        public List<string> tags;
        public string url;
        public string url2;
        public string description;
        public int approval;
        public string source;
    }
}

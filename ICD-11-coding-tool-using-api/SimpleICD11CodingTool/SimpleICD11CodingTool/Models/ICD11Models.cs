using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ICD11CodingTool.Models
{
    public class PV
    {
        public string Label { get; set; }
        //public string Id { get; set; }
        public dynamic Data { set; get; }
        public double Score { get; set; }
    }



    public class ICD11Entity
    {
        public ICD11Entity()
        {
            Children = new List<ICD11Entity>();
            PVList = new List<PV>();

        }
        public dynamic Data { get; set; }
        public string Id { get; set; }
        public string Code { get; set; }
        public string Title { get; set; }

        public List<ICD11Entity> Children { get; set; }
        public List<PV> PVList { get; set; }
        public double Score { get; set; }
        public string Chapter { get; set; }

    }

    public class WordCandidate
    {
        public string Label { get; set; }
    }

}

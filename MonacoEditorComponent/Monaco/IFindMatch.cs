using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Monaco.Monaco
{
    public interface IFindMatch
    {
        ///<Summary>
        /// 
        ///</Summary>
        void FindMatchBrand();

        ///<Summary>
        /// 
        ///</Summary>
        string[] Matches { get;   }

        ///<Summary>
        /// 
        ///</Summary>
        Range Range { get;   }
    }
}

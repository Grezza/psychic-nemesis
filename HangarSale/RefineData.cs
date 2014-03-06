using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Reflection;

namespace HangarSale
{
    class RefineData
    {
        public int TypeID;
        public int MaterialTypeID;
        public int Quantity;

        private RefineData(int TypeID, int MaterialTypeID, int Quantity)
        {
            this.TypeID = TypeID;
            this.MaterialTypeID = MaterialTypeID;
            this.Quantity = Quantity;
        }

        static List<RefineData> _All;
        public static List<RefineData> All
        {
            get
            {
                if (_All == null)
                {
                    _All = XElement.Load(Assembly.GetExecutingAssembly().GetManifestResourceStream("HangarSale.RefineData.xml")).Elements().Select(
                        e =>
                            new RefineData(int.Parse(e.Element("typeID").Value), 
                                int.Parse(e.Element("materialTypeID").Value),
                                int.Parse(e.Element("quantity").Value))).ToList();
                }
                return _All;
            }
        }
    }
}

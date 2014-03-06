using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using EveCom;
using EveComFramework.Core;
using EveComFramework.SessionControl;
using EveComFramework.Data;

namespace HangarSale
{
    #region Settings

    [Serializable]
    public class WindowSettings
    {
        public int Width = 400;
        public int Height = 340;
        public int X;
        public int Y;
        public bool Shrunk = false;
    }

    internal class Settings : EveComFramework.Core.Settings
    {
        public bool AlwaysOnTop = false;
        public string PriceBase = "Jita";
        public string Method = "Min";
        public bool Markup = false;
        public int PercentDiscount = 1;
        public int IskDiscount = 1000;
        public Settings.SerializableDictionary<string, WindowSettings> WindowSettings = new Settings.SerializableDictionary<string, WindowSettings>();
    }

    #endregion

    class Core : State
    {
        #region Instantiation

        static Core _Instance;
        public static Core Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new Core();
                }
                return _Instance;
            }
        }

        private Core()
            : base()
        {
        }

        #endregion

        #region Variables

        public Logger Console = new Logger("HangarSale");

        public Settings Config = new Settings();
        public SessionControl SessionControl = SessionControl.Instance;
        Dictionary<int, ItemInformation> BuyData = new Dictionary<int, ItemInformation>();
        Dictionary<int, ItemInformation> SellData = new Dictionary<int, ItemInformation>();

        public class ItemInformation
        {
            public decimal Average;
            public decimal Max;
            public decimal Min;
            public decimal StdDev;
            public decimal Median;
            public decimal Percentile;
            public long Volume;

            public ItemInformation(decimal Average, decimal Max, decimal Min, decimal StdDev, decimal Median, decimal Percentile, Int64 Volume)
            {
                this.Average = Average;
                this.Max = Max;
                this.Min = Min;
                this.StdDev = StdDev;
                this.Median = Median;
                this.Percentile = Percentile;
                this.Volume = Volume;
            }
        }

        #endregion

        #region Actions

        public void Enabled(bool var)
        {
            if (var)
            {
                QueueState(Initialize);
                //QueueState(GetMineralPrices);
                QueueState(ClearGreyLists);
                QueueState(PostOrders);
            }
            else
            {
                Clear();
            }
        }

        #endregion

        #region States

        bool Initialize(object[] Params)
        {
            if ((!Session.InStation) || !Session.Safe)
            {
                Console.Log("|rHangarSale needs to be used in a station!");
                Clear();
            }
            //Console.Log("|oRetrieving mineral prices");
            return true;
        }

        bool GetMineralPrices(object[] Params)
        {
            XDocument xDocument = XDocument.Load("http://api.eve-central.com/api/marketstat?typeid=34&typeid=35&typeid=36&typeid=37&typeid=38&typeid=39&typeid=40&usesystem=30000142");
            IEnumerable<XElement> data = xDocument.Root.Elements().Elements();
            foreach (XElement mineral in data)
            {
                BuyData.AddOrUpdate((int)mineral.Attribute("id"), new ItemInformation(decimal.Parse(mineral.Element("buy").Element("avg").Value), decimal.Parse(mineral.Element("buy").Element("max").Value), decimal.Parse(mineral.Element("buy").Element("min").Value), decimal.Parse(mineral.Element("buy").Element("stddev").Value), decimal.Parse(mineral.Element("buy").Element("median").Value), decimal.Parse(mineral.Element("buy").Element("percentile").Value), long.Parse(mineral.Element("buy").Element("volume").Value)));
                SellData.AddOrUpdate((int)mineral.Attribute("id"), new ItemInformation(decimal.Parse(mineral.Element("sell").Element("avg").Value), decimal.Parse(mineral.Element("sell").Element("max").Value), decimal.Parse(mineral.Element("sell").Element("min").Value), decimal.Parse(mineral.Element("sell").Element("stddev").Value), decimal.Parse(mineral.Element("sell").Element("median").Value), decimal.Parse(mineral.Element("sell").Element("percentile").Value), long.Parse(mineral.Element("sell").Element("volume").Value)));
            }
            Console.Log("|gMineral prices retrieved");
            return true;
        }

        List<Item> ItemGreyList = new List<Item>();
        List<Order> OrderGreyList = new List<Order>();

        bool ClearGreyLists(object[] Params)
        {
            if (!Station.ItemHangar.IsPrimed)
            {
                Station.ItemHangar.Prime();
                return false;
            }

            NeedsPriceUpdate = true;
            ItemGreyList.Clear();
            OrderGreyList.Clear();

            return true;
        }

        List<Group> IgnoreItems = new List<Group>
        {
            Group.AuditLogSecureContainer, 
            Group.CargoContainer, 
            Group.FreightContainer, 
            Group.SecureCargoContainer
        };
        bool NeedsPriceUpdate = true;
        bool PostOrders(object[] Params)
        {
            if (Order.MyOrdersNeedsUpdate)
            {
                Order.MyOrdersUpdate();
                return false;
            }
            if (Order.MyOrders.Count >= MaxOrders)
            {
                Console.Log("|oNo more order slots left");
                return true;
            }
            Item NextItem = Station.ItemHangar.Items.FirstOrDefault(a => !ItemGreyList.Contains(a) && !IgnoreItems.Contains(a.GroupID));
            if (NextItem == null)
            {
                Console.Log("|oAll items posted");
                return true;
            }

            if (NeedsPriceUpdate || !SellData.ContainsKey(NextItem.TypeID))
            {
                Console.Log("|oPulling data");
                Console.Log(" |-g{0}", NextItem.Type);
                string uri = string.Format("http://api.eve-central.com/api/marketstat?typeid={0}&usesystem={1}", NextItem.TypeID, SolarSystem.All.First(a => a.Name == Config.PriceBase).ID);
                XDocument xDocument = XDocument.Load(uri);
                IEnumerable<XElement> data = xDocument.Root.Elements().Elements();
                foreach (XElement mineral in data)
                {
                    BuyData.AddOrUpdate((int)mineral.Attribute("id"), new ItemInformation(decimal.Parse(mineral.Element("buy").Element("avg").Value), decimal.Parse(mineral.Element("buy").Element("max").Value), decimal.Parse(mineral.Element("buy").Element("min").Value), decimal.Parse(mineral.Element("buy").Element("stddev").Value), decimal.Parse(mineral.Element("buy").Element("median").Value), decimal.Parse(mineral.Element("buy").Element("percentile").Value), long.Parse(mineral.Element("buy").Element("volume").Value)));
                    SellData.AddOrUpdate((int)mineral.Attribute("id"), new ItemInformation(decimal.Parse(mineral.Element("sell").Element("avg").Value), decimal.Parse(mineral.Element("sell").Element("max").Value), decimal.Parse(mineral.Element("sell").Element("min").Value), decimal.Parse(mineral.Element("sell").Element("stddev").Value), decimal.Parse(mineral.Element("sell").Element("median").Value), decimal.Parse(mineral.Element("sell").Element("percentile").Value), long.Parse(mineral.Element("sell").Element("volume").Value)));
                }
                NeedsPriceUpdate = false;
                return false;
            }

            NeedsPriceUpdate = true;

            if (SellData[NextItem.TypeID].Min <= 0)
            {
                ItemGreyList.Add(NextItem);
                return false;
            }


            double pricebase = (double)SellData[NextItem.TypeID].Min;
            switch (Config.Method)
            {
                case "Max":
                    pricebase = (double)SellData[NextItem.TypeID].Max;
                    break;
                case "Average":
                    pricebase = (double)SellData[NextItem.TypeID].Average;
                    break;
                case "Median":
                    pricebase = (double)SellData[NextItem.TypeID].Median;
                    break;
            }
            double discount = pricebase * (Config.PercentDiscount * .01);

            if (Config.Markup)
            {
                Console.Log("|oPosting {0} for |g{1}", NextItem.Type, toISK(pricebase + discount));
                NextItem.Sell(pricebase + discount, Order.Duration.OneDay);
            }
            else
            {
                if (discount > Config.IskDiscount) discount = Config.IskDiscount;
                Console.Log("|oPosting {0} for |g{1}", NextItem.Type, toISK(pricebase - discount));
                NextItem.Sell(pricebase - discount, Order.Duration.OneDay);
            }
            ItemGreyList.Add(NextItem);
            

            DislodgeCurState(Blank, 10000);


            return false;
        }

        bool CheckOrders(object[] Params)
        {
            if (Order.MyOrdersNeedsUpdate)
            {
                Order.MyOrdersUpdate();
                return false;
            }

            Order NextOrder = Order.MyOrders.FirstOrDefault(a => !OrderGreyList.Contains(a));
            if (NextOrder == null)
            {
                Console.Log("|oAll orders checked");
                return true;
            }

            if (NeedsPriceUpdate || !SellData.ContainsKey(NextOrder.ItemType.TypeID))
            {
                Console.Log("|oPulling data");
                Console.Log(" |-g{0}", NextOrder.ItemType.Type);
                string uri = string.Format("http://api.eve-central.com/api/marketstat?typeid={0}&usesystem=30000142", NextOrder.ItemType.TypeID);
                XDocument xDocument = XDocument.Load(uri);
                IEnumerable<XElement> data = xDocument.Root.Elements().Elements();
                foreach (XElement mineral in data)
                {
                    BuyData.AddOrUpdate((int)mineral.Attribute("id"), new ItemInformation(decimal.Parse(mineral.Element("buy").Element("avg").Value), decimal.Parse(mineral.Element("buy").Element("max").Value), decimal.Parse(mineral.Element("buy").Element("min").Value), decimal.Parse(mineral.Element("buy").Element("stddev").Value), decimal.Parse(mineral.Element("buy").Element("median").Value), decimal.Parse(mineral.Element("buy").Element("percentile").Value), long.Parse(mineral.Element("buy").Element("volume").Value)));
                    SellData.AddOrUpdate((int)mineral.Attribute("id"), new ItemInformation(decimal.Parse(mineral.Element("sell").Element("avg").Value), decimal.Parse(mineral.Element("sell").Element("max").Value), decimal.Parse(mineral.Element("sell").Element("min").Value), decimal.Parse(mineral.Element("sell").Element("stddev").Value), decimal.Parse(mineral.Element("sell").Element("median").Value), decimal.Parse(mineral.Element("sell").Element("percentile").Value), long.Parse(mineral.Element("sell").Element("volume").Value)));
                }
                NeedsPriceUpdate = false;
                return false;
            }

            NeedsPriceUpdate = true;

            EVEFrame.Log(NextOrder.Price + "  >  " + (double)SellData[NextOrder.ItemType.TypeID].Min);

            if (NextOrder.Price > (double)SellData[NextOrder.ItemType.TypeID].Min)
            {
                double discount = (double)SellData[NextOrder.ItemType.TypeID].Min * .01;
                if (discount > 1000) discount = 1000;
                EVEFrame.Log("Discount: " + discount);
                Console.Log("|oRepricing from |r{0} to |g{1}", toISK(NextOrder.Price), toISK((double)SellData[NextOrder.ItemType.TypeID].Min - discount));
                // NextOrder.Alter((double)SellData[NextOrder.ItemType.TypeID].Min - discount);
                DislodgeCurState(Blank, 10000);
            }

            OrderGreyList.Add(NextOrder);

            return false;
        }



        bool Blank(object[] Params)
        {
            return true;
        }

        string toISK(double val)
        {
            if (val > 1000000000) return string.Format("{0:C2}b isk", val / 1000000000);
            if (val > 1000000) return string.Format("{0:C2}m isk", val / 1000000);
            if (val > 1000) return string.Format("{0:C2}k isk", val / 1000);
            return string.Format("{0:C2} isk", val);
        }

        int MaxOrders
        {
            get
            {
                int orders = 0;
                Skill Analyze = Skill.All.FirstOrDefault(a => a.Type == "Trade");
                if (Analyze != null) orders += (Analyze.SkillLevel * 4);
                Analyze = Skill.All.FirstOrDefault(a => a.Type == "Retail");
                if (Analyze != null) orders += (Analyze.SkillLevel * 8);
                Analyze = Skill.All.FirstOrDefault(a => a.Type == "Wholesale");
                if (Analyze != null) orders += (Analyze.SkillLevel * 16);
                Analyze = Skill.All.FirstOrDefault(a => a.Type == "Tycoon");
                if (Analyze != null) orders += (Analyze.SkillLevel * 32);
                return orders;
            }
        }

        #endregion
    }

    #region Utility classes

    static class DictionaryHelper
    {
        public static IDictionary<TKey, TValue> AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary[key] = value;
            }
            else
            {
                dictionary.Add(key, value);
            }

            return dictionary;
        }
    }

    public static class ForEachExtension
    {
        public static void ForEach<T>(this IEnumerable<T> items, Action<T> method)
        {
            foreach (T item in items)
            {
                method(item);
            }
        }
    }

    #endregion
}

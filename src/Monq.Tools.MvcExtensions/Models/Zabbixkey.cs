using System;
using System.Collections.Generic;
using System.Text;

namespace Monq.Tools.MvcExtensions.Models
{
    public class ZabbixKey
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id => MakeKey(ZabbixId, ElementId);

        /// <summary>
        /// Id Zabbix сервера.
        /// </summary>
        public int ZabbixId { get; protected set; }

        /// <summary>
        /// Id элемента.
        /// </summary>
        public long ElementId { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ZabbixKey"/> class.
        /// </summary>
        /// <param name="zabbixId">The zabbix identifier.</param>
        /// <param name="elementId">The element identifier.</param>
        public ZabbixKey(int zabbixId, long elementId)
        {
            ZabbixId = zabbixId;
            ElementId = elementId;
        }

        /// <summary>
        /// Метод парсит строку вида 1.2 и выделяет из нее id экземпляра zabbix и id элемента. 1 = id zabbix, 2 = elementid.
        /// </summary>
        /// <param name="id">Id элемента вида "1.22".</param>
        /// <returns></returns>
        public static ZabbixKey ParseKey(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var keys = id.Split('.');
            if (keys.Length < 2)
                return null;

            var zabbixId = int.Parse(keys[0]);
            var elementId = long.Parse(keys[1]);

            return new ZabbixKey(zabbixId, elementId);
        }

        /// <summary>
        /// Метод парсит строки вида 1.2 и выделяет из них id экземпляра zabbix и id элемента. 1 = id zabbix, 2 = elementid.
        /// </summary>
        /// <param name="ids">Набор Id элемента вbиа "1.22".</param>
        /// <returns></returns>
        public static IEnumerable<ZabbixKey> ParseKey(IEnumerable<string> ids)
        {
            var keys = new List<ZabbixKey>();
            foreach (var i in ids)
            {
                var key = ParseKey(i);
                if (key != null)
                    keys.Add(key);
            }
            return keys;
        }

        /// <summary>
        /// Создает id элемента из полей zabbixiId и elementId.
        /// </summary>
        /// <param name="zabbixId"></param>
        /// <param name="elementId"></param>
        /// <returns></returns>
        public static string MakeKey(int zabbixId, long elementId)
        {
            return $"{zabbixId}.{elementId}";
        }
    }
}
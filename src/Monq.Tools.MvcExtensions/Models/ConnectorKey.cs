using System.Collections.Generic;

namespace Monq.Tools.MvcExtensions.Models
{
    public class ConnectorKey
    {
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id => ToString();

        /// <summary>
        /// Id Zabbix сервера.
        /// </summary>
        public long ConnectorId { get; protected set; }

        /// <summary>
        /// Id элемента.
        /// </summary>
        public string ElementId { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectorKey" /> class.
        /// </summary>
        /// <param name="connectorId">The connector identifier.</param>
        /// <param name="elementId">The element identifier.</param>
        public ConnectorKey(long connectorId, string elementId)
        {
            ConnectorId = connectorId;
            ElementId = elementId;
        }

        /// <summary>
        /// Метод парсит строку вида 1.2 и выделяет из нее id экземпляра zabbix и id элемента. 1 = id zabbix, 2 = elementid.
        /// </summary>
        /// <param name="id">Id элемента вида "1.22".</param>
        /// <returns></returns>
        public static ConnectorKey ParseKey(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            var keys = id.Split('.');
            if (keys.Length < 2)
                return null;

            var zabbixId = long.Parse(keys[0]);
            var elementId = keys[1];

            return new ConnectorKey(zabbixId, elementId);
        }

        /// <summary>
        /// Метод парсит строки вида 1.2 и выделяет из них id экземпляра zabbix и id элемента. 1 = id zabbix, 2 = elementid.
        /// </summary>
        /// <param name="ids">Набор Id элемента вbиа "1.22".</param>
        /// <returns></returns>
        public static IEnumerable<ConnectorKey> ParseKey(IEnumerable<string> ids)
        {
            var keys = new List<ConnectorKey>();
            foreach (var i in ids)
            {
                var key = ParseKey(i);
                if (key != null)
                    keys.Add(key);
            }
            return keys;
        }

        public override string ToString() => $"{ConnectorId}.{ElementId}";
    }
}
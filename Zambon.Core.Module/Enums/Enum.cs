﻿using System.Xml.Serialization;
using Zambon.Core.Module.Atrributes;
using Zambon.Core.Module.Serialization;

namespace Zambon.Core.Module.Enums
{
    /// <summary>
    /// Represents ENUM used in application.
    /// </summary>
    public class Enum : BaseNode
    {
        #region XML Attributes

        /// <summary>
        /// The same Id in ENUM structure.
        /// </summary>
        [XmlAttribute, Merge]
        public string Id { get; set; }

        #endregion

        #region XML Elements

        /// <summary>
        /// Values from the ENUM.
        /// </summary>
        [XmlElement("Value")]
        public ChildItemCollection<Value> Values { get; set; }

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Enum()
        {
            Values = new ChildItemCollection<Value>(this);
        }

        #endregion
    }
}
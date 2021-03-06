﻿using System;

namespace OpenRiaServices.M2M
{
    internal class LinkTableViewAttribute : Attribute
    {
        #region Public Properties

        public Type ElementType { get; set; }

        public Type LinkTableType { get; set; }

        public string M2MPropertyName { get; set; }

        public string OtherEndAssociationName { get; set; }

        #endregion
    }
}
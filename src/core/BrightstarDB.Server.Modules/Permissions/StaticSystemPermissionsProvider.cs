﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Xml;
using Nancy.Security;

namespace BrightstarDB.Server.Modules.Permissions
{
    /// <summary>
    /// Provides a collection of statically configured system permissions.
    /// The permissions are configured using XML fragments in the service configuration section.
    /// </summary>
    public class StaticSystemPermissionsProvider : AbstractSystemPermissionsProvider
    {
        private const string UserEl = "user";
        private const string ClaimEl = "claim";
        private const string PermissionsAttr = "permissions";
        private const string NameAttr = "name";

        private readonly Dictionary<string, SystemPermissions> _userPermissions;
        private readonly Dictionary<string, SystemPermissions> _claimPermissions;

        /// <summary>
        /// Initialize a new provider with a fixed mapping of users and claims to system permissions
        /// </summary>
        /// <param name="userPermissions">A dictionary mapping user name to system permissions for that user</param>
        /// <param name="claimPermissions">A dictionary mapping a user claim to the system permissions associated with that claim</param>
        public StaticSystemPermissionsProvider(IDictionary<string, SystemPermissions> userPermissions,
                                               IDictionary<string, SystemPermissions> claimPermissions)
        {
            _userPermissions = new Dictionary<string, SystemPermissions>(userPermissions);
            _claimPermissions = new Dictionary<string, SystemPermissions>(claimPermissions);
        }

        /// <summary>
        /// Initialize a new provider that reads its configuration from the specified root configuration element
        /// </summary>
        /// <param name="configEl">The root element of the provider configuration</param>
        public StaticSystemPermissionsProvider(XmlNode configEl)
        {
            _userPermissions = new Dictionary<string, SystemPermissions>();
            _claimPermissions = new Dictionary<string, SystemPermissions>();
            if (configEl == null) throw new ArgumentNullException("configEl");
            foreach (var el in configEl.ChildNodes.OfType<XmlElement>())
            {
                if (el.LocalName.Equals(UserEl))
                {
                    ProcessPermissionsElement(el, _userPermissions);
                } else if (el.LocalName.Equals(ClaimEl))
                {
                    ProcessPermissionsElement(el, _claimPermissions);
                }
            }
        }

        private static void ProcessPermissionsElement(XmlElement permissionsElement, IDictionary<string, SystemPermissions> permissonsDict)
        {
            SystemPermissions permissions;
            if (permissionsElement.HasAttribute(NameAttr) && permissionsElement.HasAttribute(PermissionsAttr) &&
                BrightstarServiceConfigurationSectionHandler.TryGetSystemPermissionsAttributeValue(permissionsElement,
                                                                                                  PermissionsAttr,
                                                                                                  out permissions))
            {
                permissonsDict[permissionsElement.GetAttribute(NameAttr)] = permissions;
            }
        }

        public override SystemPermissions GetPermissionsForUser(ClaimsPrincipal user)
        {

            if (user == null || !user.IsAuthenticated())
            {
                return SystemPermissions.None;
            }

            var calculatedPermissions = SystemPermissions.None;
            var userName = user.FindFirst(ClaimTypes.Name)?.Value;
            if (!string.IsNullOrEmpty(userName))
            {
                // See if there are user-specific permissions
                SystemPermissions userPermissions;
                if (_userPermissions.TryGetValue(userName, out userPermissions))
                {
                    calculatedPermissions |= userPermissions;
                }
            }

            foreach (var claim in user.Claims)
            {
                if (claim.Type == ClaimTypes.Role)
                {
                    var role = claim.Value;

                    SystemPermissions claimPermissions;
                    if (_claimPermissions.TryGetValue(role, out claimPermissions))
                    {
                        calculatedPermissions |= claimPermissions;
                    }
                }
            }

            return calculatedPermissions;
        }
    }
}

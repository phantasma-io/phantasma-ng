using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Phantasma.Infrastructure.API.Structs;

namespace Phantasma.Infrastructure.API.Controllers
{
    public class OrganizationController : BaseControllerV1
    {
        [APIInfo(typeof(OrganizationResult), "Returns info about an organization.", false, 60)]
        [HttpGet("GetOrganization")]
        public OrganizationResult GetOrganization(string ID)
        {
            if ( string.IsNullOrEmpty(ID))
            {
                throw new APIException("invalid organization ID");
            }
            
            var nexus = NexusAPI.GetNexus();

            if (!nexus.OrganizationExists(nexus.RootStorage, ID))
            {
                throw new APIException("invalid organization");
            }

            var org = nexus.GetOrganizationByName(nexus.RootStorage, ID);
            var members = org.GetMembers();

            return new OrganizationResult()
            {
                id = ID,
                name = org.Name,
                members = members.Select(x => x.Text).ToArray(),
            };
        }
        
        [APIInfo(typeof(OrganizationResult), "Returns info about an organization.", false, 60)]
        [HttpGet("GetOrganizationByName")]
        public OrganizationResult GetOrganizationByName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new APIException("invalid organization name");
            }
            
            var nexus = NexusAPI.GetNexus();

            var org = nexus.GetOrganizationByName(nexus.RootStorage, name);
            var members = org.GetMembers();
            
            return new OrganizationResult()
            {
                id = org.ID,
                name = org.Name,
                members = members.Select(x => x.Text).ToArray(),
            };
        } 
        
        [APIInfo(typeof(OrganizationResult), "Returns info about all of organizations on chain.", false, 60)]
        [HttpGet("GetOrganizations")]
        public OrganizationResult[] GetOrganizations(bool extended = false)
        {
            var nexus = NexusAPI.GetNexus();

            var orgs = nexus.GetOrganizations(nexus.RootStorage);

            return orgs.Select(x =>
            {
                var org = nexus.GetOrganizationByName(nexus.RootStorage, x);
                var members = org.GetMembers();

                return new OrganizationResult()
                {
                    id = org.ID,
                    name = x,
                    members = extended ? members.Select(y => y.Text).ToArray() : new string[0],
                };
            }).ToArray();
        }
    }
}

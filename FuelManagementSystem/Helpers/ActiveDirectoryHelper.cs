using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.DirectoryServices;
using FuelManagementSystem.Models;

namespace FuelManagementSystem.ADautho
{
    public class ActiveDirectoryHelper
    {
        private FuelManagementSystemEntities db = new FuelManagementSystemEntities();
        private string Usernameeldap = "noreply";
        private string Passwordldap = "N0rep1y7ujm<KI*";
        private string eldapurl = "LDAP://10.1.72.10";


        public String AuthenticateUser(string username, string password)
        {
            String response = "";
            try
            {
                using (var entry = new DirectoryEntry(eldapurl))
                {
                    entry.Username = Usernameeldap;
                    entry.Password = Passwordldap;

                    var searcher = new DirectorySearcher(entry)
                    {
                        Filter = $"(&(objectCategory=user)(sAMAccountName={username}) (userPrincipalName={username}))"
                    };

                    var result = searcher.FindOne();
                    if (result != null)
                    {

                        if (result != null)
                            response = "true";
                        DirectoryEntry userEntry = result.GetDirectoryEntry();
                        string email = userEntry.Properties["mail"].Value.ToString();
                    }

                    return response;
                }
            }
            catch (Exception ex)
            {
                response = "An error occurred during authentication: " + ex.Message;
                return response;
            }
        }
        public string AuthenticateUsers(string username, string password)
        {
            string response = "";

            try
            {
                // Construct the LDAP path
                string ldapPath = eldapurl;

                using (var entry = new DirectoryEntry(ldapPath, username, password))
                {
                    // Attempt to bind
                    var nativeObject = entry.NativeObject;

                    response = "true"; // Auth successful

                    // Fetch additional user details if needed
                    var searcher = new DirectorySearcher(entry)
                    {
                        Filter = $"(&(objectCategory=user)(sAMAccountName={username})(userPrincipalName={username}))"
                    };
                    var result = searcher.FindOne();
                    if (result != null)
                    {
                        DirectoryEntry userEntry = result.GetDirectoryEntry();
                        string email = userEntry.Properties["mail"].Value?.ToString() ?? "";
                        // You can include email if needed
                    }
                }
            }
            catch (DirectoryServicesCOMException ex)
            {
                // Thrown when credentials are invalid
                response = "Invalid username or password.";
            }
            catch (System.Runtime.InteropServices.COMException ex)
            {
                // LDAP server unreachable or network issue
                response = "Connection failed. Please check your network or AD server.";
            }
            catch (Exception ex)
            {
                // Any other unexpected errors
                response = "An unexpected error occurred: " + ex.Message;
            }

            return response;
        }


        public List<string> GetAllGroupsFromAD()
        {
            var groups = new List<string>();

            var ldapPath = eldapurl;
            var username = Usernameeldap;
            var password = Passwordldap;

            using (var entry = new DirectoryEntry(ldapPath, username, password))
            {
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = "(objectClass=organizationalUnit)";
                    searcher.PropertiesToLoad.Add("name");

                    var results = searcher.FindAll();

                    foreach (SearchResult result in results)
                    {
                        var groupName = result.Properties["name"][0].ToString();
                        if (groupName == "Computers" || groupName == "Users" || groupName == "Workstations")
                        {
                            continue;
                        }
                        else
                        {

                            groups.Add(groupName);
                        }
                    }
                }
            }

            return groups;
        }

        //public List<departementInfodetail> departementInfodetails()
        //{
        //    List<departementInfodetail> deplist = new List<departementInfodetail>();
        //    List<string> userdepartement = GetAllGroupsFromAD();

        //    for (int i = 0; i < userdepartement.Count; i++)
        //    {
        //        var departementName = userdepartement[i];
        //        var depId = i + 1;

        //        var depiinfo = new departementInfodetail
        //        {
        //            DID = depId,
        //            DepartemntName = departementName
        //        };

        //        deplist.Add(depiinfo);
        //        var dep = db.DepartementInfoes.Where(d => d.DepartemntName == departementName);
        //        if (dep.Count() < 1)
        //        {
        //            db.DepartementInfoes.Add(new DepartementInfo
        //            {
        //                DID = depId,
        //                DepartemntName = departementName,
        //                CreatedDate = DateTime.Now,
        //                CreatedBy = "mekuanentl"


        //            });
        //        }

        //    }
        //    db.SaveChanges();
        //    return deplist;
        //}

        public List<string> GetUserListInDepartment(string departmentName)
        {
            var ldapPath = eldapurl;
            var username = Usernameeldap;
            var password = Passwordldap;


            List<string> userList = new List<string>();
            string usernamesto = "";
            using (DirectoryEntry directoryEntry = new DirectoryEntry(ldapPath, username, password))
            {
                using (DirectorySearcher directorySearcher = new DirectorySearcher(directoryEntry))
                {
                    directorySearcher.Filter = $"(&(objectCategory=user)(department={departmentName}))";
                    directorySearcher.PropertiesToLoad.Add("samAccountName");

                    SearchResultCollection searchResults = directorySearcher.FindAll();

                    foreach (SearchResult searchResult in searchResults)
                    {
                        DirectoryEntry userEntry = searchResult.GetDirectoryEntry();
                        usernamesto = userEntry.Properties["samAccountName"].Value.ToString();
                        userList.Add(usernamesto);
                    }
                }
            }

            return userList;
        }

        public List<ADUserViewModel> SearchADUsers(string keyword)
        {
            var users = new List<ADUserViewModel>();

            using (var entry = new DirectoryEntry(eldapurl, Usernameeldap, Passwordldap))
            using (var searcher = new DirectorySearcher(entry))
            {
                searcher.Filter = $"(&(objectCategory=person)(displayName=*{keyword}*))"; // Searching by FullName
                searcher.PropertiesToLoad.Add("samAccountName");
                searcher.PropertiesToLoad.Add("displayName");
                searcher.PropertiesToLoad.Add("mail");
                searcher.PropertiesToLoad.Add("department");
                searcher.PropertiesToLoad.Add("title");

                foreach (SearchResult result in searcher.FindAll())
                {
                    users.Add(new ADUserViewModel
                    {
                        Username = result.Properties["samAccountName"]?.Count > 0 ? result.Properties["samAccountName"][0].ToString() : "",
                        FullName = result.Properties["displayName"]?.Count > 0 ? result.Properties["displayName"][0].ToString() : "",
                        Email = result.Properties["mail"]?.Count > 0 ? result.Properties["mail"][0].ToString() : "",
                        ADDepartment = result.Properties["department"]?.Count > 0 ? result.Properties["department"][0].ToString() : "",
                        Position = result.Properties["title"]?.Count > 0 ? result.Properties["title"][0].ToString() : ""
                    });
                }
            }

            return users;
        }

        public ADUserViewModel GetUserByUsername(string username)
        {
            using (var entry = new DirectoryEntry(eldapurl, Usernameeldap, Passwordldap))
            using (var searcher = new DirectorySearcher(entry))
            {
                searcher.Filter = $"(&(objectCategory=person)(samAccountName={username}))";
                searcher.PropertiesToLoad.Add("samAccountName");
                searcher.PropertiesToLoad.Add("displayName");
                searcher.PropertiesToLoad.Add("mail");
                searcher.PropertiesToLoad.Add("department");
                searcher.PropertiesToLoad.Add("title");

                var result = searcher.FindOne();
                if (result != null)
                {
                    return new ADUserViewModel
                    {
                        Username = result.Properties["samAccountName"]?.Count > 0 ? result.Properties["samAccountName"][0].ToString() : "",
                        FullName = result.Properties["displayName"]?.Count > 0 ? result.Properties["displayName"][0].ToString() : "",
                        Email = result.Properties["mail"]?.Count > 0 ? result.Properties["mail"][0].ToString() : "",
                        ADDepartment = result.Properties["department"]?.Count > 0 ? result.Properties["department"][0].ToString() : "",
                        Position = result.Properties["title"]?.Count > 0 ? result.Properties["title"][0].ToString() : ""
                    };
                }

                return null;
            }
        }



        // all departement
        public string GetUserOrganizationalUnit(string username)
        {
            using (var entry = new DirectoryEntry())
            {
                entry.Path = eldapurl; // Replace with the appropriate domain controller
                entry.Username = Usernameeldap;
                entry.Password = Passwordldap;
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=user)(samaccountname={username}))";
                    searcher.PropertiesToLoad.Add("distinguishedName");

                    var result = searcher.FindOne();

                    if (result != null)
                    {
                        var distinguishedName = result.Properties["distinguishedName"][0].ToString();
                        var ou = distinguishedName.Split(new[] { "OU=" }, StringSplitOptions.None)[1];
                        return ou;
                    }
                }
            }

            return null;
        }
        public List<string> GetSubDepartments(string parentDepartment)
        {
            var subDepartments = new List<string>();

            using (var entry = new DirectoryEntry())
            {
                entry.Path = eldapurl; // Replace with the appropriate domain controller
                entry.Username = Usernameeldap;
                entry.Password = Passwordldap;
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=organizationalUnit)(ou={parentDepartment}))";
                    searcher.PropertiesToLoad.Add("ou");

                    var results = searcher.FindAll();

                    foreach (SearchResult result in results)
                    {
                        var subDepartment = result.Properties["ou"][0].ToString();
                        subDepartments.Add(subDepartment);
                    }
                }
            }

            return subDepartments;
        }

        public List<string> GetTeamsInDepartment(string department)
        {
            var teams = new List<string>();

            using (var entry = new DirectoryEntry())
            {
                entry.Path = eldapurl; // Replace with the appropriate domain controller
                entry.Username = Usernameeldap;
                entry.Password = Passwordldap;

                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=group)(department={department}))";
                    searcher.PropertiesToLoad.Add("cn");

                    var results = searcher.FindAll();

                    foreach (SearchResult result in results)
                    {
                        var team = result.Properties["cn"][0].ToString();
                        teams.Add(team);
                    }
                }
            }

            return teams;
        }

        public List<string> GetUserGroups(string username)
        {
            var groups = new List<string>();

            using (var entry = new DirectoryEntry())
            {
                entry.Path = eldapurl;// Replace with the appropriate domain controller
                entry.Username = Usernameeldap;
                entry.Password = Passwordldap;

                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=user)(samaccountname={username}))";
                    searcher.PropertiesToLoad.Add("memberOf");

                    var result = searcher.FindOne();

                    if (result != null)
                    {
                        foreach (var group in result.Properties["memberOf"])
                        {
                            var groupName = group.ToString();
                            groups.Add(groupName);
                        }
                    }
                }
            }

            return groups;
        }
        public string GetUserDepartment(string username)
        {

            using (var entry = new DirectoryEntry())
            {
                entry.Path = eldapurl; // Replace with the appropriate domain controller
                entry.Username = Usernameeldap;
                entry.Password = Passwordldap;

                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=user)(samaccountname={username}))";
                    searcher.PropertiesToLoad.Add("department");

                    var result = searcher.FindOne();

                    if (result != null)
                    {
                        var department = result.Properties["department"]?.Count > 0
                            ? result.Properties["department"][0].ToString()
                            : string.Empty;

                        return department;
                    }
                }
            }

            return string.Empty;
        }

        public string GetUserFullName(string username)
        {

            using (var entry = new DirectoryEntry())
            {
                entry.Path = eldapurl; // Replace with the appropriate domain controller
                entry.Username = Usernameeldap;
                entry.Password = Passwordldap;

                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=user)(samaccountname={username}))";
                    searcher.PropertiesToLoad.Add("displayName");

                    var result = searcher.FindOne();

                    if (result != null)
                    {
                        var fullName = result.Properties["displayName"]?.Count > 0
                            ? result.Properties["displayName"][0].ToString()
                            : string.Empty;

                        return fullName;
                    }
                }
            }

            return string.Empty;
        }
        public string GetUserPosition(string username)
        {
            using (var entry = new DirectoryEntry())
            {
                entry.Path = eldapurl; // Replace with the appropriate domain controller
                entry.Username = Usernameeldap;
                entry.Password = Passwordldap;
                using (var searcher = new DirectorySearcher(entry))
                {
                    searcher.Filter = $"(&(objectClass=user)(samaccountname={username}))";
                    searcher.PropertiesToLoad.Add("title");
                    var result = searcher.FindOne();

                    if (result != null)
                    {
                        var position = result.Properties["title"]?.Count > 0
                            ? result.Properties["title"][0].ToString()
                            : string.Empty;

                        return position;
                    }
                }
            }

            return string.Empty;
        }


        public List<string> GetAllTeamsFromAD()
        {
            var teams = new List<string>();
            string error = "";
            //var ldapPath = "LDAP://10.1.72.10/OU=Core Systems,DC=coopbank,DC=local";
            //LDAP://10.1.72.10/OU=Domain Controllers,DC=coopbank,DC=local
            var ldapPath = "LDAP://10.1.72.10/OU=Information Systems,OU=Coopbank Head Office,DC=coopbank,DC=local";
            var username = Usernameeldap;
            var password = Passwordldap;
            try
            {
                using (var entry = new DirectoryEntry(ldapPath, username, password))
                {
                    using (var searcher = new DirectorySearcher(entry))
                    {
                        searcher.Filter = "(objectClass=group)";
                        searcher.PropertiesToLoad.Add("name");

                        var results = searcher.FindAll();

                        foreach (SearchResult result in results)
                        {
                            var groupName = result.Properties["name"][0].ToString();
                            teams.Add(groupName);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                error = "An error occurred during authentication: " + ex.Message;

            }

            return teams;
        }


        public void SaveUserIfNotExists(string username)
        {
            var userExists = db.Users.Any(u => u.ADUsername == username);
            if (!userExists)
            {
                string email = "";
                string fullname = GetUserFullName(username);
                string department = GetUserDepartment(username);
                string position = GetUserPosition(username);

                // Try to get email from AD
                using (var entry = new DirectoryEntry(eldapurl, Usernameeldap, Passwordldap))
                {
                    using (var searcher = new DirectorySearcher(entry))
                    {
                        searcher.Filter = $"(&(objectClass=user)(samaccountname={username}))";
                        searcher.PropertiesToLoad.Add("mail");

                        var result = searcher.FindOne();
                        if (result != null && result.Properties["mail"].Count > 0)
                        {
                            email = result.Properties["mail"][0].ToString();
                        }
                    }
                }

                // Assign default role (e.g., RoleId = 2 => User)
                //var defaultRole = db.Roles.FirstOrDefault(r => r.role_name== "Super Admin");

                db.Users.Add(new User
                {
                    ADUsername = username,
                    FullName = fullname,
                    Email = email,
                    Position = position,

                });
                db.SaveChanges();
            }
        }

    }
}
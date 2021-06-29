using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
//using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Azure.Core.Diagnostics;
using Azure.Identity;
using Azure.ResourceManager.Compute.Models;
//using Azure.ResourceManager.Compute;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;
using Azure.ResourceManager.Compute;

namespace AzureSDKCreateRGAndVM
{
    public class ProgramVMSample
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("hello world");
            using AzureEventSourceListener listener = AzureEventSourceListener.CreateConsoleLogger(EventLevel.Error);

            var subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
            //var location = "westus2";
            var location = "westeurope";
            var nameThing = "xdmtest";

            //string resourceGroupName = RandomName("rg", 20);
            Random random = new Random();
            int randomNumber = random.Next(0, 1000);
            var resourceGroupName = $"{nameThing}{randomNumber}";
            var vmName = "vm";
            var dnsName = $"{nameThing}{randomNumber}";

            var resourcesClient = new ResourcesManagementClient(subscriptionId, new DefaultAzureCredential(),
                new ResourcesManagementClientOptions() { Diagnostics = { IsLoggingContentEnabled = true } });

            var networkClient = new NetworkManagementClient(subscriptionId, new DefaultAzureCredential());
            var computeClient = new ComputeManagementClient(subscriptionId, new DefaultAzureCredential());

            var resourceGroupClient = resourcesClient.ResourceGroups;
            var virtualNetworksClient = networkClient.VirtualNetworks;
            var publicIpAddressClient = networkClient.PublicIPAddresses;
            var networkInterfaceClient = networkClient.NetworkInterfaces;
            var virtualMachinesClient = computeClient.VirtualMachines;

            // Resource Group
            Console.WriteLine($"Creating resource group {resourceGroupName}...");
            var resourceGroup = new ResourceGroup(location);
            resourceGroup = await resourceGroupClient.CreateOrUpdateAsync(resourceGroupName, resourceGroup);

            // VNet
            Console.WriteLine($"Creating vnet ...");
            var vnet = new VirtualNetwork()
            {
                Location = location,
                AddressSpace = new AddressSpace { AddressPrefixes = new List<string> { "10.0.0.0/16" } },
                Subnets = new List<Subnet>
                {
                    new()
                    {
                        Name = "mySubnet",
                        AddressPrefix = "10.0.0.0/24",
                    }
                },
            };
            vnet = await virtualNetworksClient.StartCreateOrUpdate(resourceGroupName, "vnet", vnet).WaitForCompletionAsync();

            // Network Security Group

            // Public IP Address
            Console.WriteLine($"Creating public IP address...");
            var ipAddress = new PublicIPAddress()
            {
                PublicIPAddressVersion = Azure.ResourceManager.Network.Models.IPVersion.IPv4,
                PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
                Location = location,
                DnsSettings = new PublicIPAddressDnsSettings() { DomainNameLabel = "domainnamelabelasd123", Fqdn = dnsName, ReverseFqdn = "" }
            };
            ipAddress = await publicIpAddressClient.StartCreateOrUpdate(resourceGroupName, "publicip", ipAddress).WaitForCompletionAsync();

            // Nic
            Console.WriteLine($"Creating nic ...");
            var nic = new NetworkInterface()
            {
                Location = location,
                IpConfigurations = new List<NetworkInterfaceIPConfiguration>()
                {
                    new NetworkInterfaceIPConfiguration()
                    {
                        Name = "Primary",
                        Primary = true,
                        Subnet = new Subnet() { Id = vnet.Subnets.First().Id },
                        PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
                        PublicIPAddress = new PublicIPAddress() { Id = ipAddress.Id }
                    }
                }
            };
            nic = await networkInterfaceClient.StartCreateOrUpdate(resourceGroupName, "nic", nic).WaitForCompletionAsync();

            // VM
            Console.WriteLine($"Creating vm ...");
            var vm = new VirtualMachine(location)
            {
                NetworkProfile = new Azure.ResourceManager.Compute.Models.NetworkProfile { NetworkInterfaces = new[] { new NetworkInterfaceReference() { Id = nic.Id } } },
                OsProfile = new OSProfile
                {
                    ComputerName = "testVM",
                    AdminUsername = "username",
                    AdminPassword = "(YourPassword)",
                    LinuxConfiguration = new LinuxConfiguration { DisablePasswordAuthentication = false, ProvisionVMAgent = true }
                },
                StorageProfile = new StorageProfile()
                {
                    ImageReference = new ImageReference()
                    {
                        Offer = "UbuntuServer",
                        Publisher = "Canonical",
                        Sku = "18.04-LTS",
                        Version = "latest"
                    },
                    DataDisks = new List<DataDisk>()
                },
                HardwareProfile = new HardwareProfile() { VmSize = VirtualMachineSizeTypes.StandardB1Ms },
            };
            //vm.AvailabilitySet.Id = availabilitySet.Id;

            var operation = await virtualMachinesClient.StartCreateOrUpdateAsync(resourceGroupName, vmName, vm);
            var vmFoo = (await operation.WaitForCompletionAsync()).Value;


            Console.WriteLine("press any key to delete");
            Console.ReadKey(true);


            // Delete rg
            Console.WriteLine($"Deleting resource group {resourceGroupName}...");
            ResourceGroupsDeleteOperation deleteOperation = await resourcesClient.ResourceGroups.StartDeleteAsync(resourceGroupName);

            // don't want to wait for it to complete
            //await deleteOperation.WaitForCompletionAsync();
            Console.WriteLine("Done - may take a while for rg to delete!");
        }

        public static async Task CreateVmAsync(
           string subscriptionId,
           string resourceGroupName,
           string location,
           string vmName)
        {
            //var computeClient = new ComputeManagementClient(subscriptionId, new DefaultAzureCredential());
            //var networkClient = new NetworkManagementClient(subscriptionId, new DefaultAzureCredential());
            var resourcesClient = new ResourcesManagementClient(subscriptionId, new DefaultAzureCredential());

            //var virtualNetworksClient = networkClient.VirtualNetworks;
            //var networkInterfaceClient = networkClient.NetworkInterfaces;
            //var publicIpAddressClient = networkClient.PublicIPAddressses;
            //var availabilitySetsClient = computeClient.AvailabilitySets;
            //var virtualMachinesClient = computeClient.VirtualMachines;
            var resourceGroupClient = resourcesClient.ResourceGroups;

            // Create Resource Group
            var resourceGroup = new ResourceGroup(location);
            resourceGroup = await resourceGroupClient.CreateOrUpdateAsync(resourceGroupName, resourceGroup);

            // Create AvailabilitySet
            //var availabilitySet = new AvailabilitySet(location)
            //{
            //    PlatformUpdateDomainCount = 5,
            //    PlatformFaultDomainCount = 2,
            //    Sku = new Sku() { Name = "Aligned" }  // TODO. Verify new codegen on AvailabilitySetSkuTypes.Aligned
            //};

            //availabilitySet = await availabilitySetsClient.CreateOrUpdateAsync(resourceGroupName, vmName + "_aSet", availabilitySet);

            //// Create IP Address
            //var ipAddress = new PublicIPAddress()
            //{
            //    PublicIPAddressVersion = IPVersion.IPv4,
            //    PublicIPAllocationMethod = IPAllocationMethod.Dynamic,
            //    Location = location,
            //};

            //ipAddress = await publicIpAddressClient.StartCreateOrUpdate(resourceGroupName, vmName + "_ip", ipAddress)
            //    .WaitForCompletionAsync();

            //// Create VNet
            //var vnet = new VirtualNetwork()
            //{
            //    Location = location,
            //    AddressSpace = new AddressSpace() { AddressPrefixes = new List<string>() { "10.0.0.0/16" } },
            //    Subnets = new List<Subnet>()
            //    {
            //        new Subnet()
            //        {
            //            Name = "mySubnet",
            //            AddressPrefix = "10.0.0.0/24",
            //        }
            //    },
            //};

            //vnet = await virtualNetworksClient
            //    .StartCreateOrUpdate(resourceGroupName, vmName + "_vent", vnet)
            //    .WaitForCompletionAsync();

            //// Create Network interface
            //var nic = new NetworkInterface()
            //{
            //    Location = location,
            //    IpConfigurations = new List<NetworkInterfaceIPConfiguration>()
            //    {
            //        new NetworkInterfaceIPConfiguration()
            //        {
            //            Name = "Primary",
            //            Primary = true,
            //            Subnet = new Subnet() { Id = vnet.Subnets.First().Id },
            //            PrivateIPAllocationMethod = IPAllocationMethod.Dynamic,
            //            PublicIPAddress = new PublicIPAddress() { Id = ipAddress.Id }
            //        }
            //    }
            //};

            //nic = await networkInterfaceClient
            //    .StartCreateOrUpdate(resourceGroupName, vmName + "_nic", nic)
            //    .WaitForCompletionAsync();

            //var vm = new VirtualMachine(location)
            //{
            //    NetworkProfile = new Compute.Models.NetworkProfile { NetworkInterfaces = new[] { new NetworkInterfaceReference() { Id = nic.Id } } },
            //    OsProfile = new OSProfile
            //    {
            //        ComputerName = "testVM",
            //        AdminUsername = "username",
            //        AdminPassword = "(YourPassword)",
            //        LinuxConfiguration = new LinuxConfiguration { DisablePasswordAuthentication = false, ProvisionVMAgent = true }
            //    },
            //    StorageProfile = new StorageProfile()
            //    {
            //        ImageReference = new ImageReference()
            //        {
            //            Offer = "UbuntuServer",
            //            Publisher = "Canonical",
            //            Sku = "18.04-LTS",
            //            Version = "latest"
            //        },
            //        DataDisks = new List<DataDisk>()
            //    },
            //    HardwareProfile = new HardwareProfile() { VmSize = VirtualMachineSizeTypes.StandardB1Ms },
            //};
            //vm.AvailabilitySet.Id = availabilitySet.Id;

            //var operaiontion = await virtualMachinesClient.StartCreateOrUpdateAsync(resourceGroupName, vmName, vm);
            //var vm = (await operaiontion.WaitForCompletionAsync()).Value;
            //}
        }

        static string RandomName(string prefix, int maxLen)
        {
            var random = new Random();
            var sb = new StringBuilder(prefix);
            for (int i = 0; i < (maxLen - prefix.Length); i++)
                sb.Append(random.Next(10).ToString());
            return sb.ToString();
        }
    }
}

# PCIDeviceRepository

PCIDeviceRepository is a REST API designed to allow easy programmatic access to the [PCI ID Repository](https://pci-ids.ucw.cz/).

This is accomplished by periodically downloading the PCI ID repository database file, parsing it, and storing it in a database for easy lookup.

The application consists of two main components which are designed to run directly in Azure Apps and Azure Functions with the device database being stored in Cosmos DB. These are: 
* RepositoryProcessor - An Azure Function which retrieves, parses, and inserts the device data into the database.
* RepositoryAPI - An Azure App REST API to provide access to the device data.

## Configuration

### Database

Both the processor function and the API require access to a Cosmos DB account for storing and retrieving the device database. This is configured with the following environment variables:

```
DatabaseConfiguration__Uri = <the URI of your Cosmos DB endpoint>
DatabaseConfiguration__PrimaryKey = <Cosmos DB access key>
DatabaseConfiguration__DatabaseName = <the name of the database to be used in Cosmos DB>
```

Note that the API component only requires read access to the database, so a read only key should be used. Additionally, the processor function will automatically create the database with a manual throughput limit of 1000RU/s at the database level. As such you should not attempt to create the database yourself inside of Cosmos DB.

### PCI ID Repository

The processor function also requires the URL for the PCI device repository database file that will be parsed. This can be taken from the above link, or from any other source that supplies the `pci.ids` file.

```
Configuration__RepositoryUrl = <the location of the pci.ids file>
```

## Usage

### Repository version

Get the version of the PCI database

#### Request
`GET /api/repository`

#### Response

```json
{
    "version": "2024-06-23T00:00:00",
    "lastUpdate": "2024-08-08T20:16:50.8741057Z"
}
```

### Vendors

Lists all known vendors

#### Request

`GET /api/vendors`

#### Response

```json
[
    {
        "id": "8086",
        "name": "Intel Corporation"
    },
    ...
]
```

#### Vendor lookup by ID

The endpoint can also be used to look up a specific vendor if the vendor ID is known

`GET /api/vendors/8086` 

```json
{
    "id": "8086",
    "name": "Intel Corporation"
}
```

### Devices

Lists the known devices

#### Request

`GET /api/devices`

#### Response

```json
[
    {
        "vendorId": "8086",
        "vendorName": "Intel Corporation",
        "deviceId": "0007",
        "deviceName": "82379AB",
        "subdevices": []
    },
    {
        "vendorId": "8086",
        "vendorName": "Intel Corporation",
        "deviceId": "0008",
        "deviceName": "Extended Express System Support Controller",
        "subdevices": []
    },
    {
        "vendorId": "8086",
        "vendorName": "Intel Corporation",
        "deviceId": "0039",
        "deviceName": "21145 Fast Ethernet",
        "subdevices": []
    },
    ...
]
```

#### Device lookup by ID
The endpoint can also be used to look up a specific device if both the vendor ID and the device ID are known

`GET /api/devices/8086/0007` 

```json
{
    "vendorId": "8086",
    "vendorName": "Intel Corporation",
    "deviceId": "0007",
    "deviceName": "82379AB",
    "subdevices": []
}
```

### Device Classes

Lists the known device classes

#### Request
`GET /api/classes`

#### Response
```json
[
    {
        "id": "00",
        "name": "Unclassified device",
        "deviceSubclasses": [
            {
                "id": "00",
                "name": "Non-VGA unclassified device",
                "programmingInterfaces": []
            },
            {
                "id": "01",
                "name": "VGA compatible unclassified device",
                "programmingInterfaces": []
            },
            {
                "id": "05",
                "name": "Image coprocessor",
                "programmingInterfaces": []
            }
        ]
    },
	...
]
```

#### Class lookup by ID
The endpoint can also be used to look up a specific device class if the class ID is known

`GET /api/classes/00`

```json
{
    "id": "00",
    "name": "Unclassified device",
    "deviceSubclasses": [
        {
            "id": "00",
            "name": "Non-VGA unclassified device",
            "programmingInterfaces": []
        },
        {
            "id": "01",
            "name": "VGA compatible unclassified device",
            "programmingInterfaces": []
        },
        {
            "id": "05",
            "name": "Image coprocessor",
            "programmingInterfaces": []
        }
    ]
}
```

## License

[MIT](https://choosealicense.com/licenses/mit/)
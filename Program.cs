using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PintAPI;
using PintAPI.Models;

var builder = WebApplication.CreateBuilder(args);

// Add a database context
builder.Services.AddDbContext<PintApiDb>(
    options =>
    {
        string connectionString = builder.Configuration.GetConnectionString("PintAppleDb");
        options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
    });

var app = builder.Build();

string GeneratePasswordHash(string password)
{
    using HashAlgorithm algorithm = SHA256.Create();
    byte[] hashedPassword = algorithm.ComputeHash(
        Encoding.UTF8.GetBytes(password)
    );
        
    StringBuilder stringBuilder = new StringBuilder();
    foreach (byte b in hashedPassword)
    {
        stringBuilder.Append(b.ToString("X2"));
    }

    return stringBuilder.ToString();
}


app.MapGet("/", async () =>
{
    string html = await File.ReadAllTextAsync(Path.Combine(Directory.GetCurrentDirectory(), "index.html"));

    return Results.Extensions.Html(@html);
});

app.MapPost("/register", async (PintApiDb db, [FromBody] Dictionary<string, string> json) =>
{
    if (!json.ContainsKey("email"))
    {
        return Results.Problem("Missing e-mail.");
    }
    
    if (!json.ContainsKey("password"))
    {
        return Results.Problem("Missing password.");
    }
    
    if (!json.ContainsKey("address"))
    {
        return Results.Problem("Missing address.");
    }
    
    CareGroup newGroup = new CareGroup
    {
        Email = json["email"],
        PasswordHash = GeneratePasswordHash(json["password"]),
        Address = json["address"],
        ApiKey = Guid
            .NewGuid()
            .ToString()
            .Replace("-","") + Guid
            .NewGuid()
            .ToString()
            .Replace("-","")
    };

    await db.CareGroups.AddAsync(newGroup);
    await db.SaveChangesAsync();
    
    return Results.Created("/register", newGroup.ApiKey);
});

app.MapGet("/login", async (PintApiDb db, [FromBody] Dictionary<string, string> json) => {
    if (!json.ContainsKey("email"))
    {
        return Results.Problem("Missing e-mail.");
    }
    
    if (!json.ContainsKey("password"))
    {
        return Results.Problem("Missing password.");
    }

    string password = json["password"];
    string passwordHash = GeneratePasswordHash(password);

    CareGroup? careGroup = await db.CareGroups.FirstOrDefaultAsync(
        careGroup => careGroup.Email.ToLower().Equals(json["email"].ToLower())
                     && careGroup.PasswordHash.Equals(passwordHash)
    );

    if (careGroup == null)
    {
        return Results.NotFound("Unknown username or password.");
    }

    return Results.Ok(careGroup.ApiKey);
});

app.MapGet("/care_groups/{identifier?}",
    async (PintApiDb db, [FromBody] Dictionary<string, string> json, [FromQuery] string? identifier) =>
    {
        if (!json.ContainsKey("adminKey"))
        {
            return Results.Problem("Missing adminKey.");
        }
    
        Admin? admin = await db.Admins.FirstOrDefaultAsync(admin => admin.Key == json["adminKey"]);

        if (admin == null)
        {
            return Results.Problem("Invalid admin key.");
        }

        if (identifier == null)
        {
            return Results.Ok(
                await db.CareGroups.ToListAsync()
            );
        }
        
        // Filter on deviceId
        if (uint.TryParse(identifier, out uint careGroupId))
        {
            return Results.Ok(
                await db.CareGroups
                    .Where(careGroup => careGroup.CareGroupId == careGroupId)
                    .ToListAsync()
            );
        }

        return Results.Ok(await db.CareGroups
            .Where(careGroup => careGroup.Email.ToLower().Contains(identifier.ToLower()))
            .ToListAsync()
        );
    });

// TODO: do not return caregroup info
// This returns the devices a CareGroup has
// You must specify dictionary<type, type> so that ASP.NET understand the body is JSON
app.MapGet("/devices/{identifier?}", async (PintApiDb db, [FromBody] Dictionary<string, string> json, [FromQuery] string? identifier) =>
{
    if (!json.ContainsKey("apiKey"))
    {
        return Results.Problem("Missing apiKey.");
    }

    string apiKey = json["apiKey"];

    CareGroup? careGroup = await db.CareGroups.FirstOrDefaultAsync(careGroup => careGroup.ApiKey == apiKey);
    
    if (careGroup == null)
    {
        return Results.Problem("Invalid API key.");
    }

    // No filter, return everything
    if (identifier == null)
    {
        return Results.Ok(
            await db.PatientDevices
                .Where(patientDevice => patientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
                .Include(patientDevice => patientDevice.Patient)
                .ToListAsync()
        );
    }
    
    // Filter on deviceId
    if (uint.TryParse(identifier, out uint deviceId))
    {
        return Results.Ok(
            await db.PatientDevices
                .Where(patientDevice => patientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
                .Where(patientDevice => patientDevice.Device.DeviceId == deviceId)
                .ToListAsync()
        );
    }

    // Filter on EspId
    return Results.Ok(
        await db.PatientDevices
            .Where(patientDevice => patientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
            .Where(patientDevice => patientDevice.Device.FriendlyName.ToLower().Contains(identifier.ToLower()))
            .ToListAsync()
    );
});

// Search by patientId or event.Type
app.MapGet("/events/{identifier?}", async (PintApiDb db, [FromBody] Dictionary<string, string> json, [FromQuery] string? identifier) =>
{
    if (!json.ContainsKey("apiKey"))
    {
        return Results.Problem("Missing apiKey.");
    }

    string apiKey = json["apiKey"];

    CareGroup? careGroup = await db.CareGroups.FirstOrDefaultAsync(careGroup => careGroup.ApiKey == apiKey);
    
    if (careGroup == null)
    {
        return Results.Problem("Invalid API key.");
    }

    if (identifier == null)
    {
        // Return all events
        return Results.Ok(await db.Events
            .Where(@event => @event.PatientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
            .Include(@event => @event.PatientDevice)
            .Include(@event => @event.PatientDevice.Patient)
            .Include(@event => @event.PatientDevice.Device)
            .OrderByDescending(@event => @event.EventId)
            .ToListAsync()
        );
    }
    
    if (uint.TryParse(identifier, out uint patientId))
    {
        // Return matching patientId
        return Results.Ok(
            await db.Events
                .Where(@event => @event.PatientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
                .Where(@event => @event.PatientDevice.Patient.PatientId == patientId)
                .Include(@event => @event.PatientDevice)
                .Include(@event => @event.PatientDevice.Patient)
                .Include(@event => @event.PatientDevice.Device)
                .OrderByDescending(@event => @event.EventId)
                .ToListAsync()
        );
    }

    // Return matching identifier
    return Results.Ok(
        await db.Events
            .Where(@event => @event.PatientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
            .Where(@event => @event.PatientDevice.Device.Identifier.ToLower().Contains(identifier.ToLower()))
            .Include(@event => @event.PatientDevice)
            .Include(@event => @event.PatientDevice.Patient)
            .Include(@event => @event.PatientDevice.Device)
            .OrderByDescending(@event => @event.EventId)
            .ToListAsync()
    );
});

// Search by patientId or EspId
app.MapGet("/logs/{identifier?}", async (PintApiDb db, [FromBody] Dictionary<string, string> json, [FromQuery] string? identifier) =>
{
    if (!json.ContainsKey("apiKey"))
    {
        return Results.Problem("Missing apiKey.");
    }

    string apiKey = json["apiKey"];

    CareGroup? careGroup = await db.CareGroups.FirstOrDefaultAsync(careGroup => careGroup.ApiKey == apiKey);
    
    if (careGroup == null)
    {
        return Results.Problem("Invalid API key.");
    }

    if (identifier == null)
    {
        return Results.Ok(
            await db.Logs
                .Where(log => log.PatientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
                .Include(log => log.PatientDevice)
                .Include(log => log.PatientDevice.Patient)
                .OrderByDescending(log => log.LogId)
                .ToListAsync()
        );
    }
    
    if (uint.TryParse(identifier, out var patientId))
    {
        return Results.Ok(
            await db.Logs
                .Where(log => log.PatientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
                .Where(log => log.PatientDevice.Patient.PatientId == patientId)
                .Include(log => log.PatientDevice)
                .Include(log => log.PatientDevice.Patient)
                .OrderByDescending(log => log.LogId)
                .ToListAsync()
        );
    }

    return Results.Ok(
        await db.Logs
            .Where(log => log.PatientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
            .Where(log => log.PatientDevice.Device.Identifier.ToLower().Contains(identifier.ToLower()))
            .Include(log => log.PatientDevice)
            .Include(log => log.PatientDevice.Patient)
            .OrderByDescending(log => log.LogId)
            .ToListAsync()
    );
});

// Search by patientId, user or device
app.MapGet("/patient_devices/{identifier?}", async (PintApiDb db, [FromBody] Dictionary<string, string> json, [FromQuery] string? identifier) =>
{
    if (!json.ContainsKey("apiKey"))
    {
        return Results.Problem("Missing apiKey.");
    }

    string apiKey = json["apiKey"];

    CareGroup? careGroup = await db.CareGroups.FirstOrDefaultAsync(careGroup => careGroup.ApiKey == apiKey);
    
    if (careGroup == null)
    {
        return Results.Problem("Invalid API key.");
    }

    if (identifier == null)
    {
        return Results.Ok(
            await db.PatientDevices
                .Where(patientDevice => patientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
                .Include(patientDevice => patientDevice.Patient)
                .Include(patientDevice => patientDevice.Device)
                .ToListAsync()
        );
    }
    
    if (uint.TryParse(identifier, out uint userDeviceId))
    {
        return Results.Ok(
            await db.PatientDevices
                .Where(patientDevice => patientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
                .Where(patientDevice => patientDevice.PatientDeviceId == userDeviceId)
                .Include(patientDevice => patientDevice.Patient)
                .Include(patientDevice => patientDevice.Device)
                .ToListAsync()
        );
    }

    // We need these includes to show the full device and user objects in our response json
    // otherwise, these will be returned with a null value, while being set properly in database
    return Results.Ok(
        await db.PatientDevices
            .Where(patientDevice => patientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
            .Where(patientDevice => patientDevice.Device.Identifier.ToLower().Contains(identifier.ToLower())
                                    || patientDevice.Patient.FirstName.ToLower().Contains(identifier.ToLower())
                                    || patientDevice.Patient.LastName.ToLower().Contains(identifier.ToLower())
            )
            .Include(patientDevice => patientDevice.Patient)
            .Include(patientDevice => patientDevice.Device)
            .ToListAsync()
    );
});

// This endpoint filters on userId, userFirstName or userLastName
app.MapGet("/patients/{identifier?}", async (PintApiDb db, [FromBody] Dictionary<string, string> json, [FromQuery] string? identifier) =>
{
    if (!json.ContainsKey("apiKey"))
    {
        return Results.Problem("Missing apiKey.");
    }

    string apiKey = json["apiKey"];

    CareGroup? careGroup = await db.CareGroups.FirstOrDefaultAsync(careGroup => careGroup.ApiKey == apiKey);
    
    if (careGroup == null)
    {
        return Results.Problem("Invalid API key.");
    }

    // Return everything
    if (identifier == null)
    {
        return Results.Ok(
            await db.PatientDevices
                .Where(patientDevice => patientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
                .Select(patientDevice => patientDevice.Patient)
                .ToListAsync()
        );
    }
    
    // Filter on userId
    if (uint.TryParse(identifier, out uint patientId))
    {
        return Results.Ok(
            await db.PatientDevices
                .Where(patientDevice => patientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
                .Where(patientDevice => patientDevice.Patient.PatientId == patientId)
                .ToListAsync()
        );
    }

    return Results.Ok(
        await db.PatientDevices
            .Where(patientDevice => patientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
            .Where(
                patientDevice => patientDevice.Patient.FirstName.ToLower().Contains(identifier.ToLower())
                                 || patientDevice.Patient.LastName.ToLower().Contains(identifier.ToLower())
            )
            .ToListAsync()
    );
});

app.MapPost("/device", async (PintApiDb db, [FromBody] Dictionary<string, string> json) =>
{
    if (!json.ContainsKey("adminKey"))
    {
        return Results.Problem("Missing adminKey.");
    }
    
    Admin? admin = await db.Admins.FirstOrDefaultAsync(admin => admin.Key == json["adminKey"]);

    if (admin == null)
    {
        return Results.Problem("Invalid admin key.");
    }
    
    if (!json.ContainsKey("friendlyName"))
    {
        return Results.Problem("Missing friendlyName.");
    }

    Device newDevice = new Device
    {
        FriendlyName = json["friendlyName"],
        Identifier = Guid.NewGuid()
            .ToString()
            .Split("-")
            .First(),
        CreatedBy = admin
    };

    await db.Devices.AddAsync(newDevice);
    await db.SaveChangesAsync();
    
    return Results.Created("/device", newDevice);
});

app.MapPost("/event", async (PintApiDb db, [FromBody] Dictionary<string, string> json) =>
{
    if (!json.ContainsKey("apiKey"))
    {
        return Results.Problem("Missing apiKey.");
    }

    string apiKey = json["apiKey"];

    CareGroup? careGroup = await db.CareGroups.FirstOrDefaultAsync(careGroup => careGroup.ApiKey == apiKey);
    
    if (careGroup == null)
    {
        return Results.Problem("Invalid API key.");
    }   

    if (!json.ContainsKey("patientDeviceId"))
    {
        return Results.Problem("Missing patientDeviceId.");
    }

    bool validPatientDeviceId = uint.TryParse(json["patientDeviceId"], out uint patientDeviceId);
    
    if (!validPatientDeviceId)
    {
        return Results.Problem("Invalid patientDeviceId.");
    }

    PatientDevice? patientDevice = await db.PatientDevices
        .Where(patientDevice => patientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
        .FirstOrDefaultAsync(
            patientDevice => patientDevice.PatientDeviceId == patientDeviceId
        );

    if (patientDevice == null)
    {
        return Results.Problem("Unknown patientDeviceId for this care group.");
    }
    
    if (!json.ContainsKey("timestamp"))
    {
        return Results.Problem("Missing timestamp.");
    }

    bool timestampValid = DateTime.TryParse(json["timestamp"], out DateTime timeStamp);

    if (!timestampValid)
    {
        return Results.Problem("Timestamp format unparseable.");
    }
    
    if (!json.ContainsKey("type"))
    {
        return Results.Problem("Missing eventType.");
    }

    Event newEvent = new Event
    {
        PatientDevice = patientDevice,
        Timestamp = timeStamp,
        Type = json["type"]
    };

    await db.Events.AddAsync(newEvent);
    await db.SaveChangesAsync();
    // include user and device in result    
    // return Results.Created("/event", newEvent);
    return Results.Ok(
        await db.Events
            .Where(@event => @event.EventId == newEvent.EventId)
            .Include(@event => @event.PatientDevice)
            .Include(@event => @event.PatientDevice.Patient)
            .Include(@event => @event.PatientDevice.Device)
            .ToListAsync()
    );
});

app.MapPost("/log", async (PintApiDb db, [FromBody] Dictionary<string, string> json) =>
{
    if (!json.ContainsKey("apiKey"))
    {
        return Results.Problem("Missing apiKey.");
    }

    string apiKey = json["apiKey"];

    CareGroup? careGroup = await db.CareGroups.FirstOrDefaultAsync(careGroup => careGroup.ApiKey == apiKey);
    
    if (careGroup == null)
    {
        return Results.Problem("Invalid API key.");
    }
    
    if (!json.ContainsKey("patientDeviceId"))
    {
        return Results.Problem("Missing patientDeviceId.");
    }

    bool validPatientDevice = uint.TryParse(json["patientDeviceId"], out uint patientDeviceId);
    
    if (!validPatientDevice)
    {
        return Results.Problem("Invalid patientDeviceId.");
    }

    PatientDevice? patientDevice = await db.PatientDevices
        .Where(patientDevice => patientDevice.Patient.CareGroup.CareGroupId == careGroup.CareGroupId)
        .FirstOrDefaultAsync(
            patientDevice => patientDevice.PatientDeviceId == patientDeviceId
        );

    if (patientDevice == null)
    {
        return Results.Problem("Unknown patientDeviceId for this care group.");
    }

    if (!json.ContainsKey("timestamp"))
    {
        return Results.Problem("Missing timestamp.");
    }
    
    bool timestampValid = DateTime.TryParse(json["timestamp"], out DateTime timeStamp);

    if (!timestampValid)
    {
        return Results.Problem("Timestamp format unparseable.");
    }
    
    Log newLog = new Log
    {
        PatientDevice = patientDevice,
        Timestamp = timeStamp,
    };
    
    if (json.ContainsKey("heartbeat"))
    {
        string heartbeat = json["heartbeat"];
        
        bool validHeartbeat = uint.TryParse(heartbeat, out uint heartbeatValue);
    
        if (!validHeartbeat)
        {
            return Results.Problem("Heartbeat has to be a positive integer");
        }

        newLog.Heartbeat = heartbeatValue;
    }
    
    if (json.ContainsKey("battery"))
    {

        string battery = json["battery"];
        
        bool validBattery = uint.TryParse(battery, out uint batteryValue);
    
        if (!validBattery || batteryValue > 100)
        {
            return Results.Problem("Battery has to be a positive integer from 0 to 100");
        }

        newLog.Battery = batteryValue;
    }
    
    await db.Logs.AddAsync(newLog);
    await db.SaveChangesAsync();

    return Results.Ok(
        await db.Logs
            .Where(log => log.LogId == newLog.LogId)
            .Include(log => log.PatientDevice)
            .Include(log => log.PatientDevice.Patient)
            .Include(log => log.PatientDevice.Device)
            .ToListAsync()
    );
});

app.MapPost("/match", async (PintApiDb db, [FromBody] Dictionary<string, string> json) =>
{
    if (!json.ContainsKey("apiKey"))
    {
        return Results.Problem("Missing apiKey.");
    }

    string apiKey = json["apiKey"];

    CareGroup? careGroup = await db.CareGroups.FirstOrDefaultAsync(careGroup => careGroup.ApiKey == apiKey);
    
    if (careGroup == null)
    {
        return Results.Problem("Invalid API key.");
    }
    
    // Check if json body is complete
    if (!json.ContainsKey("patientId"))
    {
        return Results.Problem("Missing patientId.");
    }
    
    bool validUserId = uint.TryParse(json["patientId"], out uint patientId);
    
    if (!validUserId)
    {
        return Results.Problem("Invalid patientId.");
    }

    Patient? patient = await db.Patients
        .Where(patient => patient.CareGroup.CareGroupId == careGroup.CareGroupId)
        .FirstOrDefaultAsync(
            patient => patient.PatientId == patientId
        );

    if (patient == null)
    {
        return Results.Problem("Unknown patientId for this care group.");
    }

    // Check device Id valid
    if (!json.ContainsKey("deviceId"))
    {
        return Results.Problem("Missing deviceId.");
    }
    
    bool validDeviceId = uint.TryParse(json["deviceId"], out uint deviceId);
    
    if (!validDeviceId)
    {
        return Results.Problem("Invalid deviceId.");
    }

    Device? device = await db.Devices
        .FirstOrDefaultAsync(
            device => device.DeviceId == deviceId
        );

    if (device == null)
    {
        return Results.Problem("Unknown deviceId for this care group.");
    }

    // Check if device or user are already matched
    bool matched = db.PatientDevices
        .Any(patientDevice =>
            patientDevice.Device.DeviceId == deviceId &&
            patientDevice.Patient.PatientId == patientId
        );

    if (matched)
    {
        return Results.Problem("Device or patient already mapped");
    }

    // No matches yet, add a new one
    PatientDevice newMatch = new PatientDevice
    {
        Device = device,
        Patient = patient
    };

    await db.PatientDevices.AddAsync(newMatch);
    await db.SaveChangesAsync();

    return Results.Created("/match", 
        await db.PatientDevices
            .Where(patientDevice => patientDevice.PatientDeviceId == newMatch.PatientDeviceId)
            .Include(patientDevice => patientDevice.Device)
            .Include(patientDevice => patientDevice.Patient)
            .FirstAsync()
    );
});

app.MapPost("/patient", async (PintApiDb db, [FromBody] Dictionary<string, string> json) =>
{
    if (!json.ContainsKey("apiKey"))
    {
        return Results.Problem("Missing apiKey.");
    }

    string apiKey = json["apiKey"];

    CareGroup? careGroup = await db.CareGroups.FirstOrDefaultAsync(careGroup => careGroup.ApiKey == apiKey);
    
    if (careGroup == null)
    {
        return Results.Problem("Invalid API key.");
    }
    
    // Check device Id valid
    if (!json.ContainsKey("firstName"))
    {
        return Results.Problem("Missing firstName.");
    }
    
    // Check device Id valid
    if (!json.ContainsKey("lastName"))
    {
        return Results.Problem("Missing lastName.");
    }

    Patient newPatient = new Patient
    {
        FirstName = json["firstName"],
        LastName = json["lastName"],
        CareGroup = careGroup,
        Notes = json["notes"]
    };
    
    if (json.ContainsKey("dateOfBirth"))
    {
        bool dateOfBirthValid = DateTime.TryParse(json["dateOfBirth"], out DateTime dateOfBirth);

        if (!dateOfBirthValid)
        {
            return Results.Problem("Date of birth format unparseable.");
        }

        newPatient.DateOfBirth = dateOfBirth;
    }
    
    await db.Patients.AddAsync(newPatient);
    await db.SaveChangesAsync();
    return Results.Created("/user", newPatient);
});

app.MapPost("/admin", async (PintApiDb db, Dictionary<string, string> json) =>
{
    if (!json.ContainsKey("adminKey"))
    {
        return Results.Problem("Missing adminKey.");
    }
    
    Admin? admin = await db.Admins.FirstOrDefaultAsync(admin => admin.Key == json["adminKey"]);

    if (admin == null)
    {
        return Results.Problem("Invalid admin key.");
    }

    if (!json.ContainsKey("firstName"))
    {
        return Results.Problem("Missing firstName.");
    }
    
    if (!json.ContainsKey("lastName"))
    {
        return Results.Problem("Missing lastName.");
    }

    Admin newAdmin = new Admin
    {
        FirstName = json["firstName"],
        LastName = json["lastName"],
        Key = Guid
            .NewGuid()
            .ToString()
            .Replace("-", "") + Guid
            .NewGuid()
            .ToString()
            .Replace("-", "")
    };

    await db.Admins.AddAsync(newAdmin);

    await db.SaveChangesAsync();

    return Results.Created("/admin", newAdmin);
});

app.MapGet("/guid", () => Results.Ok(
    Guid
        .NewGuid()
        .ToString()
        .Replace("-", "") + Guid
        .NewGuid()
        .ToString()
        .Replace("-", "")
));

app.Run();

static class ResultsExtensions
{
    public static IResult Html(this IResultExtensions resultExtensions, string html)
    {
        ArgumentNullException.ThrowIfNull(resultExtensions);

        return new HtmlResult(html);
    }
}

class HtmlResult : IResult
{
    private readonly string _html;

    public HtmlResult(string html)
    {
        _html = html;
    }

    public Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.ContentType = MediaTypeNames.Text.Html;
        httpContext.Response.ContentLength = Encoding.UTF8.GetByteCount(_html);
        return httpContext.Response.WriteAsync(_html);
    }
}
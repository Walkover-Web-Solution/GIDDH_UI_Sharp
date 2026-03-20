# Environment-Specific Memory Configuration

## Overview
Memory reservation is now automatically configured based on the environment your application runs in.

## Configuration by Environment

### **Development (Local)**
- **File:** `appsettings.json` or `appsettings.Development.json`
- **Memory Reservation:** Disabled
- **Reason:** Saves local machine resources during development

```json
{
  "MemoryReservation": {
    "Enabled": false
  }
}
```

### **Test/Staging**
- **File:** `appsettings.Test.json`
- **Memory Reservation:** **1GB** (enabled)
- **Reason:** Moderate allocation for testing environment

```json
{
  "MemoryReservation": {
    "Enabled": true,
    "ReservedMemoryBytes": 1073741824
  }
}
```

### **Production**
- **File:** `appsettings.Production.json`
- **Memory Reservation:** **2GB** (enabled)
- **Reason:** Full allocation for production performance

```json
{
  "MemoryReservation": {
    "Enabled": true,
    "ReservedMemoryBytes": 2147483648
  }
}
```

## How It Works

.NET automatically loads the correct appsettings file based on the `ASPNETCORE_ENVIRONMENT` variable:

```bash
# Development (default)
ASPNETCORE_ENVIRONMENT=Development
→ Loads: appsettings.json + appsettings.Development.json
→ Memory: Disabled

# Test
ASPNETCORE_ENVIRONMENT=Test
→ Loads: appsettings.json + appsettings.Test.json
→ Memory: 1GB reserved

# Production
ASPNETCORE_ENVIRONMENT=Production
→ Loads: appsettings.json + appsettings.Production.json
→ Memory: 2GB reserved
```

## Deployment Examples

### **Local Development**
```bash
# No environment variable needed (defaults to Development)
dotnet run
# Memory reservation: Disabled
```

### **Test Environment**
```bash
export ASPNETCORE_ENVIRONMENT=Test
dotnet run
# Memory reservation: 1GB
```

### **Production**
```bash
export ASPNETCORE_ENVIRONMENT=Production
dotnet run
# Memory reservation: 2GB
```

### **Docker - Test**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
ENV ASPNETCORE_ENVIRONMENT=Test
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "GiddhTemplate.dll"]
```

```bash
docker run -d -m 2g your-image:latest
# Memory reservation: 1GB (Test environment)
```

### **Docker - Production**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
ENV ASPNETCORE_ENVIRONMENT=Production
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "GiddhTemplate.dll"]
```

```bash
docker run -d -m 4g your-image:latest
# Memory reservation: 2GB (Production environment)
```

### **Kubernetes - Test**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: giddh-template-test
spec:
  template:
    spec:
      containers:
      - name: app
        image: your-image:latest
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Test"
        resources:
          limits:
            memory: "2Gi"
          requests:
            memory: "1Gi"
```

### **Kubernetes - Production**
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: giddh-template-prod
spec:
  template:
    spec:
      containers:
      - name: app
        image: your-image:latest
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        resources:
          limits:
            memory: "4Gi"
          requests:
            memory: "2Gi"
```

### **AWS Elastic Beanstalk**

**.ebextensions/environment.config:**
```yaml
option_settings:
  - namespace: aws:elasticbeanstalk:application:environment
    option_name: ASPNETCORE_ENVIRONMENT
    value: "Production"
```

This will automatically use `appsettings.Production.json` with 2GB reservation.

### **systemd Service**

**Test:**
```ini
[Service]
Environment="ASPNETCORE_ENVIRONMENT=Test"
ExecStart=/usr/bin/dotnet /opt/giddh-template/GiddhTemplate.dll
```

**Production:**
```ini
[Service]
Environment="ASPNETCORE_ENVIRONMENT=Production"
ExecStart=/usr/bin/dotnet /opt/giddh-template/GiddhTemplate.dll
```

## Verify Configuration

After deployment, check which configuration is active:

```bash
curl http://localhost:5000/api/diagnostics/memory
```

**Test Environment Response:**
```json
{
  "reservation": {
    "enabled": true,
    "reservedMemoryMB": 1024.00
  }
}
```

**Production Environment Response:**
```json
{
  "reservation": {
    "enabled": true,
    "reservedMemoryMB": 2048.00
  }
}
```

**Development Response:**
```json
{
  "reservation": {
    "enabled": false,
    "reservedMemoryMB": 0
  }
}
```

## Startup Logs

**Test Environment:**
```
[11:30:00 INF] Environment: Test
[11:30:00 INF] Reserving 1024.00 MB of memory at startup...
[11:30:01 INF] Memory reservation complete. Reserved: 1024.00 MB
```

**Production Environment:**
```
[11:30:00 INF] Environment: Production
[11:30:00 INF] Reserving 2048.00 MB of memory at startup...
[11:30:02 INF] Memory reservation complete. Reserved: 2048.00 MB
```

**Development:**
```
[11:30:00 INF] Environment: Development
[11:30:00 INF] Memory reservation is disabled
```

## Customizing for Other Environments

You can create additional environment-specific files:

### **Staging Environment**
Create `appsettings.Staging.json`:
```json
{
  "MemoryReservation": {
    "Enabled": true,
    "ReservedMemoryBytes": 1610612736
  }
}
```

Then set:
```bash
export ASPNETCORE_ENVIRONMENT=Staging
```

### **QA Environment**
Create `appsettings.QA.json`:
```json
{
  "MemoryReservation": {
    "Enabled": true,
    "ReservedMemoryBytes": 536870912
  }
}
```

## Override via Environment Variables

You can still override settings using environment variables:

```bash
# Override Test environment to use 1.5GB instead of 1GB
export ASPNETCORE_ENVIRONMENT=Test
export MemoryReservation__ReservedMemoryBytes=1610612736
dotnet run
```

Environment variables take precedence over appsettings files.

## Summary

| Environment | File | Memory Reserved | Enabled |
|-------------|------|-----------------|---------|
| **Development** | appsettings.Development.json | 0 | ❌ No |
| **Test** | appsettings.Test.json | 1GB | ✅ Yes |
| **Production** | appsettings.Production.json | 2GB | ✅ Yes |

**No manual configuration needed** - just set `ASPNETCORE_ENVIRONMENT` and the correct settings are automatically applied!

## Files Created

- ✅ `appsettings.Production.json` - Production config (2GB)
- ✅ `appsettings.Test.json` - Test config (1GB)
- ✅ `appsettings.json` - Base config (Disabled)
- ✅ All files registered in `GiddhTemplate.csproj`

**Ready to deploy!** 🚀

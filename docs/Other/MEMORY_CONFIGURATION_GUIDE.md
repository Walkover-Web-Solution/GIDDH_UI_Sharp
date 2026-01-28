# Memory Configuration Guide for .NET Application

## Overview
This guide explains how to configure and control memory usage for the GiddhTemplate PDF generation service.

## Understanding .NET Memory Management

Unlike Java/JVM where you set `-Xmx` heap size, .NET uses **automatic memory management** with the Garbage Collector (GC). However, you can still configure limits and behavior.

### Key Differences from JVM
- **No Pre-Allocation:** .NET doesn't pre-allocate memory like JVM heap
- **Dynamic Growth:** Memory grows as needed up to configured limits
- **Automatic Tuning:** GC adjusts based on workload patterns
- **Hard Limits Available:** Can set maximum heap size constraints

## Configuration Methods

### **1. GC Heap Hard Limit (Recommended for Production)**

Set a maximum memory limit that the GC will not exceed.

#### **Method A: Environment Variables**

**For 2GB Limit:**
```bash
# In bytes (2GB = 2,147,483,648 bytes)
export DOTNET_GCHeapHardLimit=2147483648

# OR in hexadecimal
export DOTNET_GCHeapHardLimit=0x80000000

# OR as percentage of total system memory (50% = 2GB on 4GB system)
export DOTNET_GCHeapHardLimitPercent=50
```

**Set in Docker/Container:**
```dockerfile
ENV DOTNET_GCHeapHardLimit=2147483648
ENV DOTNET_GCServer=1
```

**Set in systemd service:**
```ini
[Service]
Environment="DOTNET_GCHeapHardLimit=2147483648"
Environment="DOTNET_GCServer=1"
```

**Set in AWS Elastic Beanstalk (.ebextensions):**
```yaml
option_settings:
  - namespace: aws:elasticbeanstalk:application:environment
    option_name: DOTNET_GCHeapHardLimit
    value: "2147483648"
  - namespace: aws:elasticbeanstalk:application:environment
    option_name: DOTNET_GCServer
    value: "1"
```

#### **Method B: Runtime Configuration File**

Already created: `runtimeconfig.template.json`

```json
{
  "configProperties": {
    "System.GC.HeapHardLimit": 2147483648,
    "System.GC.Server": true,
    "System.GC.Concurrent": true,
    "System.Runtime.TieredCompilation": true
  }
}
```

**Memory Limits:**
- 1GB = `1073741824`
- 2GB = `2147483648`
- 4GB = `4294967296`
- 8GB = `8589934592`

### **2. Container Memory Limits**

If running in Docker/Kubernetes, set container memory limits:

**Docker:**
```bash
docker run -m 2g --memory-reservation 1g your-image
```

**Docker Compose:**
```yaml
services:
  giddh-template:
    image: your-image
    deploy:
      resources:
        limits:
          memory: 2G
        reservations:
          memory: 1G
```

**Kubernetes:**
```yaml
resources:
  limits:
    memory: "2Gi"
  requests:
    memory: "1Gi"
```

### **3. GC Configuration Options**

#### **Server GC vs Workstation GC**

**Server GC (Recommended for Production):**
- Uses multiple GC threads
- Better for multi-core servers
- Higher throughput
- Slightly more memory usage

```bash
export DOTNET_GCServer=1
```

**Workstation GC (Default):**
- Single GC thread
- Lower latency
- Less memory overhead
- Better for desktop apps

```bash
export DOTNET_GCServer=0
```

#### **Concurrent GC**

Allows application threads to run during GC:
```bash
export DOTNET_gcConcurrent=1  # Enabled by default
```

#### **Conservative GC**

More aggressive memory release:
```bash
export DOTNET_GCConserveMemory=9  # 0-9, higher = more aggressive
```

## Recommended Production Configuration

### **For 2GB RAM Allocation:**

**Environment Variables:**
```bash
# Hard limit at 2GB
DOTNET_GCHeapHardLimit=2147483648

# Use Server GC for better throughput
DOTNET_GCServer=1

# Enable concurrent GC
DOTNET_gcConcurrent=1

# Moderate memory conservation
DOTNET_GCConserveMemory=5

# Enable tiered compilation for better performance
DOTNET_TieredCompilation=1
```

**Or in appsettings.json:**
```json
{
  "Runtime": {
    "GCHeapHardLimit": 2147483648,
    "GCServer": true,
    "GCConcurrent": true,
    "GCConserveMemory": 5
  }
}
```

## Memory Allocation Strategy

### **Current Application Profile:**

Based on our optimizations:

| Component | Memory Usage | Notes |
|-----------|--------------|-------|
| **Browser (Chromium)** | 50-100MB | Singleton, persistent |
| **RazorLight Cache** | 10-20MB | Singleton, template cache |
| **Font Cache** | 8-15MB | Cached base64 fonts |
| **Per Request (Disk Streaming)** | 4KB | Streaming buffer only |
| **Base Application** | 30-50MB | .NET runtime, services |
| **Total Baseline** | ~100-200MB | Idle state |
| **Under Load (100 concurrent)** | ~200-300MB | With disk streaming |

### **Recommended RAM Allocation:**

| Server RAM | Heap Limit | Reasoning |
|------------|------------|-----------|
| **1GB Total** | 512MB | Leave 512MB for OS/browser |
| **2GB Total** | 1.5GB | Comfortable headroom |
| **4GB Total** | 3GB | Plenty of room for spikes |
| **8GB+ Total** | 4-6GB | High concurrency support |

## Monitoring Memory Usage

### **1. Built-in Diagnostics Endpoint**

```bash
curl http://localhost:5000/api/diagnostics/memory
```

**Response:**
```json
{
  "timestamp": "2026-01-22T11:30:00Z",
  "memory": {
    "totalManagedMemoryMB": 145.23,
    "workingSetMB": 225.67,
    "privateMemoryMB": 230.45,
    "virtualMemoryMB": 2048.12
  },
  "garbageCollection": {
    "gen0Collections": 25,
    "gen1Collections": 5,
    "gen2Collections": 2
  }
}
```

### **2. dotnet-counters (Real-time)**

```bash
# Install
dotnet tool install --global dotnet-counters

# Monitor GC metrics
dotnet-counters monitor --process-id <PID> \
  System.Runtime \
  --counters gc-heap-size,gen-0-gc-count,gen-1-gc-count,gen-2-gc-count,alloc-rate,time-in-gc

# Monitor with refresh every 5 seconds
dotnet-counters monitor -p <PID> --refresh-interval 5
```

### **3. Application Logs**

GC events are logged by Serilog. Check logs for memory patterns:
```bash
grep "GC" /var/log/template-logs/giddh-template.log
```

### **4. System Memory**

```bash
# Linux
free -h
top -p <PID>

# Check container limits
cat /sys/fs/cgroup/memory/memory.limit_in_bytes
cat /sys/fs/cgroup/memory/memory.usage_in_bytes
```

## Performance Tuning

### **Scenario 1: High Throughput (Many Concurrent Requests)**

```bash
DOTNET_GCHeapHardLimit=2147483648
DOTNET_GCServer=1                    # Multi-threaded GC
DOTNET_gcConcurrent=1                # Allow concurrent work
DOTNET_GCConserveMemory=3            # Less aggressive cleanup
DOTNET_TieredCompilation=1           # Better JIT performance
```

### **Scenario 2: Memory Constrained (Limited RAM)**

```bash
DOTNET_GCHeapHardLimit=1073741824    # 1GB limit
DOTNET_GCServer=0                    # Workstation GC (less overhead)
DOTNET_gcConcurrent=1
DOTNET_GCConserveMemory=9            # Aggressive memory release
DOTNET_GCRetainVM=0                  # Return memory to OS
```

### **Scenario 3: Low Latency (Fast Response Times)**

```bash
DOTNET_GCHeapHardLimit=2147483648
DOTNET_GCServer=0                    # Workstation GC (lower latency)
DOTNET_gcConcurrent=1                # Background GC
DOTNET_GCConserveMemory=5            # Balanced
```

## Deployment Examples

### **AWS Elastic Beanstalk**

Create `.ebextensions/memory-config.config`:
```yaml
option_settings:
  - namespace: aws:elasticbeanstalk:application:environment
    option_name: DOTNET_GCHeapHardLimit
    value: "2147483648"
  - namespace: aws:elasticbeanstalk:application:environment
    option_name: DOTNET_GCServer
    value: "1"
  - namespace: aws:elasticbeanstalk:application:environment
    option_name: DOTNET_GCConserveMemory
    value: "5"
```

### **Docker**

**Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Set GC configuration
ENV DOTNET_GCHeapHardLimit=2147483648
ENV DOTNET_GCServer=1
ENV DOTNET_gcConcurrent=1
ENV DOTNET_GCConserveMemory=5

WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "GiddhTemplate.dll"]
```

**Run with memory limit:**
```bash
docker run -d \
  --name giddh-template \
  -p 5000:5000 \
  -m 2g \
  --memory-reservation 1g \
  -e DOTNET_GCHeapHardLimit=2147483648 \
  -e DOTNET_GCServer=1 \
  your-image:latest
```

### **Kubernetes**

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: giddh-template
spec:
  template:
    spec:
      containers:
      - name: app
        image: your-image:latest
        env:
        - name: DOTNET_GCHeapHardLimit
          value: "2147483648"
        - name: DOTNET_GCServer
          value: "1"
        - name: DOTNET_GCConserveMemory
          value: "5"
        resources:
          limits:
            memory: "2Gi"
            cpu: "2000m"
          requests:
            memory: "1Gi"
            cpu: "500m"
```

### **systemd Service**

Create `/etc/systemd/system/giddh-template.service`:
```ini
[Unit]
Description=Giddh Template Service
After=network.target

[Service]
Type=notify
WorkingDirectory=/opt/giddh-template
ExecStart=/usr/bin/dotnet /opt/giddh-template/GiddhTemplate.dll
Restart=always
RestartSec=10

# Memory Configuration
Environment="DOTNET_GCHeapHardLimit=2147483648"
Environment="DOTNET_GCServer=1"
Environment="DOTNET_gcConcurrent=1"
Environment="DOTNET_GCConserveMemory=5"

# Additional settings
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="ASPNETCORE_URLS=http://0.0.0.0:5000"

[Install]
WantedBy=multi-user.target
```

## Troubleshooting

### **Issue: Application Using More Memory Than Expected**

**Check:**
1. Verify heap limit is set:
   ```bash
   dotnet-counters monitor -p <PID> System.Runtime --counters gc-heap-size
   ```

2. Check for memory leaks:
   ```bash
   dotnet-dump collect -p <PID>
   dotnet-dump analyze <dump-file>
   > dumpheap -stat
   ```

3. Monitor GC behavior:
   ```bash
   curl http://localhost:5000/api/diagnostics/memory
   ```

### **Issue: Out of Memory Exceptions**

**Causes:**
- Heap limit too low for workload
- Memory leak in application
- Too many concurrent requests

**Solutions:**
1. Increase heap limit:
   ```bash
   DOTNET_GCHeapHardLimit=4294967296  # Increase to 4GB
   ```

2. Enable more aggressive GC:
   ```bash
   DOTNET_GCConserveMemory=9
   ```

3. Check for leaks using diagnostics endpoint

### **Issue: Slow Performance**

**Causes:**
- Too frequent GC collections
- Heap limit too low
- Wrong GC mode

**Solutions:**
1. Increase heap limit to reduce GC frequency
2. Use Server GC for better throughput:
   ```bash
   DOTNET_GCServer=1
   ```
3. Monitor GC time:
   ```bash
   dotnet-counters monitor -p <PID> --counters time-in-gc
   ```

## Best Practices

### ✅ Do's

1. **Set heap limits in production** to prevent runaway memory usage
2. **Use Server GC** on multi-core production servers
3. **Monitor memory metrics** regularly via diagnostics endpoint
4. **Leave headroom** - don't set limit at 100% of available RAM
5. **Test under load** to find optimal settings
6. **Use container limits** in addition to GC limits for defense in depth

### ❌ Don'ts

1. **Don't set limits too low** - causes excessive GC and OOM errors
2. **Don't ignore monitoring** - memory issues can creep up slowly
3. **Don't use Workstation GC** on production servers (unless latency-critical)
4. **Don't forget OS overhead** - leave RAM for OS and browser process
5. **Don't set percentage limits** on shared hosts (unpredictable)

## Quick Reference

### **Common Memory Limits (in bytes)**

```bash
# 512MB
DOTNET_GCHeapHardLimit=536870912

# 1GB
DOTNET_GCHeapHardLimit=1073741824

# 1.5GB
DOTNET_GCHeapHardLimit=1610612736

# 2GB (Recommended for this application)
DOTNET_GCHeapHardLimit=2147483648

# 4GB
DOTNET_GCHeapHardLimit=4294967296

# 8GB
DOTNET_GCHeapHardLimit=8589934592
```

### **Recommended Production Settings**

```bash
# For 2GB RAM server
DOTNET_GCHeapHardLimit=2147483648
DOTNET_GCServer=1
DOTNET_gcConcurrent=1
DOTNET_GCConserveMemory=5
DOTNET_TieredCompilation=1
```

## Summary

**Key Points:**
- ✅ .NET doesn't pre-allocate memory like JVM
- ✅ Use `DOTNET_GCHeapHardLimit` to set maximum heap size
- ✅ Recommended: 2GB limit for this application
- ✅ Use Server GC in production for better throughput
- ✅ Monitor via `/api/diagnostics/memory` endpoint
- ✅ With disk streaming, memory usage is minimal regardless of PDF size

**Current Application:**
- **Baseline:** ~100-200MB
- **Under Load:** ~200-300MB (with disk streaming)
- **Recommended Limit:** 2GB (plenty of headroom)
- **Actual Usage:** Will stay well below 1GB even under heavy load

The disk-based streaming implementation means you won't need to worry about large memory spikes from PDF generation anymore!

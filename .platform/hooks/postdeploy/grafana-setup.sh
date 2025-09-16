#!/bin/bash
set -e

GRAFANA_APP_ENV=$(/opt/elasticbeanstalk/bin/get-config environment -k ENVIRONMENT)
SERVER_REGION=$(/opt/elasticbeanstalk/bin/get-config environment -k SERVER_REGION)
ALLOY_CONF_DIR="/etc/alloy"

# write endpoints.json (GRAFANA_APP_ENV will be expanded)
sudo tee ${ALLOY_CONF_DIR}/endpoints.json > /dev/null <<EOF
{
      "environment": "${GRAFANA_APP_ENV}",
      "server_region": "${SERVER_REGION}",
      "company": "Walkover",
      "product": "Giddh",
      "service_name": "giddh-template",
      "orgId": "14",
      "logs": {
        "url": "http://loghub.msg91.com:3100/loki/api/v1/push"
      },
      "metrics": {
        "url": "http://loghub.msg91.com:9009/api/v1/push"
      },
      "tempo": {
        "url": "http://loghub.msg91.com:4317"
      },
      "pyroscope": {
         "url": "http://loghub.msg91.com:4040"
       }
}
EOF

sudo tee ${ALLOY_CONF_DIR}/config.alloy > /dev/null <<'EOF'
local.file "endpoints" {
      filename = "/etc/alloy/endpoints.json"
}

// loki config for logs
local.file_match "logs" {
      path_targets = [
          {
            "job"       = "nginx",
            "namespace" = "/var/log/nginx",
            "__path__"  = "/var/log/nginx/*.log",
          },
          {
            "job"       = "application",
            "namespace" = "/var/log/template-logs",
            "__path__"  = "/var/log/template-logs/*.log",
          },
      ]
      ignore_older_than = "24h"
      sync_period       = "10s"
}

loki.source.file "log_source_file" {
      targets       = local.file_match.logs.targets
      forward_to    = [loki.process.log_process.receiver]
      tail_from_end = true
}

loki.process "log_process" {
      forward_to = [loki.write.log_write.receiver]
      stage.decolorize {}
      stage.static_labels {
          values = {
            env           = json_path(local.file.endpoints.content, ".environment")[0],
            server_region = json_path(local.file.endpoints.content, ".server_region")[0],
            company       = json_path(local.file.endpoints.content, ".company")[0],
            product       = json_path(local.file.endpoints.content, ".product")[0],
            service_name  = json_path(local.file.endpoints.content, ".service_name")[0],
            instance      = constants.hostname,
          }
      }
}

loki.write "log_write" {
      endpoint {
        url = json_path(local.file.endpoints.content, ".logs.url")[0]
        headers = {
          "X-Scope-OrgID" = json_path(local.file.endpoints.content, ".orgId")[0],
        }
        retry_on_http_429 = true
      }
}

// prometheus config for system monitering
prometheus.exporter.unix "metrics" {
      disable_collectors = []
      include_exporter_metrics = true

      filesystem {
        fs_types_exclude     = "^(autofs|binfmt_misc|bpf|cgroup2?|configfs|debugfs|devpts|devtmpfs|fusectl|hugetlbfs|iso9660|mqueue|nsfs|overlay|proc|procfs|pstore|rpc_pipefs|securityfs|selinuxfs|squashfs|sysfs|tracefs)$"
        mount_points_exclude = "^/(dev|proc|run/credentials/.+|sys|var/lib/docker/.+)($|/)"
        mount_timeout        = "5s"
      }

      netclass {
        ignored_devices = "^(veth.*|cali.*|[a-f0-9]{15})$"
      }

      netdev {
        device_exclude = "^(veth.*|cali.*|[a-f0-9]{15})$"
      }
}

prometheus.scrape "metrics_scrape" {
      scrape_interval = "15s"
      targets         = prometheus.exporter.unix.metrics.targets
      forward_to      = [prometheus.relabel.metrics_relabel.receiver]
}

prometheus.relabel "metrics_relabel" {
      forward_to = [prometheus.remote_write.metrics_write.receiver]
}

prometheus.remote_write "metrics_write" {
      endpoint {
        url     = json_path(local.file.endpoints.content, ".metrics.url")[0]
        headers = {
          "X-Scope-OrgID" = json_path(local.file.endpoints.content, ".orgId")[0],
        }
      }

  external_labels = {
      env           = json_path(local.file.endpoints.content, ".environment")[0],
      server_region = json_path(local.file.endpoints.content, ".server_region")[0],
      company       = json_path(local.file.endpoints.content, ".company")[0],
      product       = json_path(local.file.endpoints.content, ".product")[0],
      service_name  = json_path(local.file.endpoints.content, ".service_name")[0],
      instance      = constants.hostname,
  }
}
EOF

echo "[INFO] Restarting alloy service..."
sudo systemctl daemon-reexec
sudo systemctl restart alloy
echo "[INFO] Grafana setup complete."

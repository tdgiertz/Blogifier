LOCAL_IMAGE = "myramblings"
PROJECT = "myramblings"
SERVICE = "myramblings"

tool "run-local" do
  flag :no_cache
  include :exec, exit_on_nonzero_status: true
  def run
    cache_args = no_cache ? ["--pull", "--no-cache"] : []
    exec ["docker", "build"] + cache_args +
         ["-t", LOCAL_IMAGE, "-f", "Dockerfile", "."]
    puts "Running on http://localhost:8080"
    exec ["docker", "run", "--rm", "-it", "-p", "8080:80", "-v=/home/tim/workspace/google/kms-svc-service-account.json:/secrets/key.json", "-e=GOOGLE_APPLICATION_CREDENTIALS=/secrets/key.json", LOCAL_IMAGE]
  end
end

tool "deploy" do
  flag :tag, default: Time.new.strftime("%Y-%m-%d-%H%M%S")
  include :exec, exit_on_nonzero_status: true
  def run
    image = "gcr.io/#{PROJECT}/#{SERVICE}:#{tag}"
    exec ["gcloud", "builds", "submit", "--project", PROJECT,
          "--config", "cloudbuild.yaml",
          "--substitutions", "_IMAGE=#{image}"]
    exec ["gcloud", "run", "deploy", SERVICE,
          "--project", PROJECT, "--platform", "managed",
          "--region", "us-central1", "--allow-unauthenticated",
          "--image", image, "--concurrency", "80", "--port", "80"]
  end
end

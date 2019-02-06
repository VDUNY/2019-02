# Tailer Console App

A simple console program that monitors HTTP traffic logs.

The app is a simple .NET Core Console application (optionally) packaged into a docker container.

By default, when started, it ignores the current content of the file, and tracks new log lines as they're added.

## Build Instructions

To build you can use docker-compose, docker, or .NET core:

```PowerShell
docker-compose build
```

```PowerShell
docker build . --file .\Tailer\Dockerfile -t Tailer
```

```PowerShell
dotnet build .\Tailer.sln
```

## Run Instructions

To run you can also use docker-compose, docker, or .NET core:

```PowerShell
docker-compose up
```

```PowerShell
docker run -it --name Tailer_1 -v $pwd\data:/tmp Tailer
```

```PowerShell
dotnet run -p .\Tailer\Tailer.csproj -- -p .\data\access.log
```

In any case, you can then write log data to the `access.log` file in the data folder to see it parsed by the Tailer. For example, using the [fake apache log generator](https://github.com/kiritbasu/Fake-Apache-Log-Generator), we could run something like this:

```PowerShell
python apache-fake-log-gen.py -o CONSOLE -n 0 -s 0.2 >> .\data\access.log
```

### Parameters

Note that if you run the command with `-h` or `--help` (or `-?`) you'll get the parameter help:

```
Usage: Tailer [options]

Options:
  -p|--path <PATH>            The path to watch (defaults to /tmp/access.log)
  -e|--parse-existing         Parse existing content in the file
  -i|--interval <INTERVAL>    The number of seconds between statistics reports (defaults to 10)
  -w|--window <WINDOW>        The number of seconds of traffic which trigger threshold limits (defaults to 120)
  -t|--threshold <THRESHOLD>  The limit of requests per second that triggers high traffic alerts (defaults to 10)
  -?|-h|--help                Show help information
```

For most purposes, only the `--path` and `--threshold` parameters matter.

## The goals:

1. Consume an actively written-to [w3c-formatted HTTP access log](https://www.w3.org/Daemon/User/Config/Logging.html).
2. It should default to reading /tmp/access.log, but be overrideable
3. We must be able to keep the app running, and monitor the log file continuously
4. Should default to reading from the tail of the file

It should display statistics and alerts:

1. Display statistics every 10 seconds about the traffic during those 10 seconds (i.e. since the last display):
  - The sections of the website with the most hits (i.e. the root folder of the hit)
  - Interesting statistics on traffic as a whole
2. Display alerts when average total traffic (over a 2 minute window) exceeds a threshold.
  - Threshold should default to 10 requests per second, but be overrideable
  - Message should be “High traffic generated an alert - hits = {value}, triggered at {time}”
  - When average drops below, add a message indicating the alert recovered
  - Don't overwrite or hide these alert messages

There should be tests!
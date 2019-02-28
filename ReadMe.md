# Tailer Console App

A simple console program that monitors HTTP traffic logs.

The app is a simple .NET Core Console application (optionally) packaged into a docker container.

By default, when started, it ignores the current content of the file, and tracks new log lines as they're added.

### Future Work

There are a few improvements which could be done to improve the reporting of this project:

#### 1. Further smoothing of the statistics and alerts in three ways

The current implementation has a minor glitch in the way it calculates the interval: Because it supports parsing
the events from an existing log file, it shows stats for every ten seconds _worth_ of logs that it has parsed.
However, because the existence of traffic is not guaranteed, it can also trigger from elapsed wall-clock time.
Obviously in the case of parsing logs, that means you get hundreds of reports per second, but additionally,
on ocassion, this can cause two reports to appear somewhat closer together in real time than expected. I could
remove the wall-clock timer, or the handling of log times to alleviate this.

The current implementation calculates the "total traffic" by taking the sum of all hits over the last 2 minutes,
divided by the number of seconds (120) to get the requests per second.  This results in a situation where if the
threshold is set to 10 per second, and you're exactly at the threshold, ONE message more in the same second will
put you over, but in the next second, just TWO messages less will put you back under the threshold -- if the
traffic is very close to the exact threshold, you could bounce back and forth a lot. Making the threshold have a
higher value for high traffic than for recovering would ease that in the same way a thermostat gives a few
degrees between when it comes on and off.

Additionally, the current definition of average means that even if there's no traffic, a huge spike can send it
over the threshold instantly, and if the spike is big enough, it could take the full two minutes to drop back
down, even if there's no further traffic. Requiring some certain number of the reporting intervals within the
window to be above (or below) the threshold to trigger a change might allow those spikes to be ignored, or could
be combined to prevent cooling off too quickly.

#### 2. Better formatting

If there was some guarantee of consistency about paths and user name maximum lengths, the reports could be
formatted in columns instead of comma-separated lists, which would make them much easier to read and review.

Additionally, the current coloring implementation (which highlights warnings) depends on NLog, which doesn't pass
it's color through the docker output (at least on Windows). Switching to ASCII escape sequences for color could
make those warnings more visible. Switching to json output from NLog could make formatting them and parsing them
even easier.

#### 3. Configuration

Given more time, it would be good to store the desired parameters for the application in a config file. Having
to pass in parameters to override the defaults can grow tiresome.


## Build Instructions

To build you can use docker-compose, docker, or .NET core:

```PowerShell
docker-compose build
```

```PowerShell
docker build . --file .\Tailer\Dockerfile -t Tailer
```

```PowerShell
dotnet build .\Tailer.sln -c Release
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
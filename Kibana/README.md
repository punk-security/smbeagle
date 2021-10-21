# Introduction
This section will explain how to import the SMBeagle Kibana pattern and Dashboard.

Using the import actions, you can create objects in a Kibana instances. You can import multiple objects in a single operation.

SMBeagle uses a custom Kibana pattern, which allows dynamic creation, updates and deletion of Elastic indexes, and a custom dashboard to easy consumption of the SMBeagle data.

## Pre-requities
You will need need to install (Docker desktop)[https://www.docker.com/products/docker-desktop], and set up a folder to host your presistant elastic data.

### Persistance 
Create directory for elastic to store persistant data, do one fo the following.

Linux
```
/bin/bash
mkdir ~/elasticsearch
chown -R 1000:1000 ~/elasticsearch
```

Windows
```
cmd.exe
mkdir c:\elastcisearch
```

Inside the docker-compose.yml file we have defaulted to C:\elasticsearch, but to customise it edit the file and change the following line; 

```
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:7.14.2
    ...
    volumes:
      # - ~/elasticsearch:/usr/share/elasticsearch/data # Linux mapping
      - C:\elasticsearch:/usr/share/elasticsearch/data # Windows mapping
```

The volume config is split in to two parts {local folder}:{docker folder}, only edit the local folder section.

### Networking
In our quick start up guide we will use a custom docker-compose file, to stand up elastic and kibana service. OUr docker-compose service will only expose TCP 9200 (elasticsearch) and TCP 5601 (Kibana) to your localhost (127.0.0.1).

The network can be altered to be exposed to your network by editing the docker-compose.yml file 

To expose elastic change **127.0.0.1:9200:9200** to **0.0.0.0:9200:9200**
```
services:
  elasticsearch:
    image: docker.elastic.co/elasticsearch/elasticsearch:7.14.2
    ...
    ports:
    - "0.0.0.0:9200:9200"
```

To expose kibana change **127.0.0.1:9200:9200** to **0.0.0.0:5601:5601**
```
  kibana:
    image: docker.elastic.co/kibana/kibana:7.14.2
    ...
    ports:
    - "0.0.0.0:5601:5601"
```

# Quick start for Elastic docker
## Deploy Elasticsearch and Kibana
Once you have installed Docker desktop and setup your persistance folder.  You can now start up elastic with one simple command

1. Open a command prompt or bash terminal
2. change directory to the folder containing the docker-compose.yml file
3. run the following

```
docker-compose up -d
```

## Test Elastic is up and running
To test elastic run the following command;

```
curl 127.0.0.1:9200
```

To test kibana open a browser and connect to;

http://127.0.0.1:5601

## Deploy Kibana dashboard
Once you have deployed elsatic, you can how install the elastic dashbaords and start collecting data.

1. Download the SMBeagle **export.ndjson** file
2. Open your Kibana web management console e.g. http://127.0.0.1:5601/
3. Click on **Stack Management** > **Saved Objects**
4. Click on **import**
5. Click on the **import** under **select a file to import**
6. Highlight the **export.ndjson** file and click open
7. Click on the **Import** button at the bottomw of the screen
8. Click on **Done**
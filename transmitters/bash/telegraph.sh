#!/usr/bin/bash
SERVER=localhost
PORT=9090
MESSAGE="bash"

curl http://$SERVER:$PORT/transmission -d $MESSAGE

#!/bin/bash
# reset all nodes 
TROOT=/app/testnet/
DIR="/app/testnet/node0/data/"

# Clear old screens
screen -ls |  grep 'node' | grep '(Detached)' | awk '{print $1}' | xargs -I % -t screen -X -S % quit
screen -wipe
pkill -f "tendermint"

# Move config files
cp $TROOT/node0/config_node0.json $TROOT/node0/publish/config.json
cp $TROOT/node1/config_node1.json $TROOT/node1/publish/config.json
cp $TROOT/node2/config_node2.json $TROOT/node2/publish/config.json
cp $TROOT/node3/config_node3.json $TROOT/node3/publish/config.json

# start all tendermint sessions
screen -S node0p -dm bash -c 'cd "$(TROOT)"/node0/publish/; ./phantasma-node; exec sh'
screen -S node1p -dm bash -c 'cd "$(TROOT)"/node1/publish/; ./phantasma-node; exec sh'
screen -S node2p -dm bash -c 'cd "$(TROOT)"/node2/publish/; ./phantasma-node; exec sh'
screen -S node3p -dm bash -c 'cd "$(TROOT)"/node3/publish/; ./phantasma-node; exec sh'

#screen -rd node0p
#/bin/bash
tail -f /dev/null

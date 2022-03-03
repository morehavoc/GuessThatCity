#!/bin/bash

# Update
apt-get update -y

# Install required items
apt-get install -y ca-certificates curl gnupg lsb-release

# Install Dockers' gpg key
curl -fsSL https://download.docker.com/linux/ubuntu/gpg | sudo gpg --dearmor -o /usr/share/keyrings/docker-archive-keyring.gpg

# Stable Docker repository
echo \
  "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/docker-archive-keyring.gpg] https://download.docker.com/linux/ubuntu \
  $(lsb_release -cs) stable" | sudo tee /etc/apt/sources.list.d/docker.list > /dev/null

# Install Docker Engine
apt-get update -y
apt-get install -y docker-ce docker-ce-cli containerd.io

# Configure logging
cp ./daemon.json /etc/docker/daemon.json

systemctl restart docker.service
systemctl restart containerd.service

# Configure to start on boot
systemctl enable docker.service
systemctl enable containerd.service




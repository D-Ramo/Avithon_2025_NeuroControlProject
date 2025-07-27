package com.avithon.neurocontrolapp;

public class Config {
    String apiIp = "192.168.1.10";
    String apiPort = "8000";
    String multicastIp = "192.168.1.10";
    String multicastGroup = "239.0.0.0";
    String multicastPort = "4446";
    String additional;

    public String getApiIp() {
        return apiIp;
    }

    public void setApiIp(String apiIp) {
        this.apiIp = apiIp;
    }

    public String getApiPort() {
        return apiPort;
    }

    public void setApiPort(String apiPort) {
        this.apiPort = apiPort;
    }

    public String getMulticastIp() {
        return multicastIp;
    }

    public void setMulticastIp(String multicastIp) {
        this.multicastIp = multicastIp;
    }

    public String getMulticastGroup() {
        return multicastGroup;
    }

    public void setMulticastGroup(String multicastGroup) {
        this.multicastGroup = multicastGroup;
    }

    public String getMulticastPort() {
        return multicastPort;
    }

    public void setMulticastPort(String multicastPort) {
        this.multicastPort = multicastPort;
    }

    public String getAdditional() {
        return additional;
    }

    public void setAdditional(String additional) {
        this.additional = additional;
    }
}

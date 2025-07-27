package com.avithon.neurocontrolapp;

import java.io.IOException;
import java.net.*;
import java.util.concurrent.Executors;
import java.util.logging.Level;

public class MulticastConnection {
    private String groupAddress = "230.0.0.0"; // Must be in 224.0.0.0 to 239.255.255.255
    private String ipAddress = "192.168.8.1"; // Must be in 224.0.0.0 to 239.255.255.255
    private int port = 4446;
    private MulticastSocket socket;
    private Thread thread;

    public void connect(Cwp cwp) {
        // Create socket and join group

        int attempts = 0;

        try {
            if (HelloApplication.executor.isShutdown()) {
                HelloApplication.executor = Executors.newFixedThreadPool(10);
            }
            socket = new MulticastSocket(Integer.parseInt(HelloApplication.config.multicastPort));
            SocketAddress group = new InetSocketAddress(InetAddress.getByName(HelloApplication.config.multicastGroup), Integer.parseInt(HelloApplication.config.multicastPort));
            socket.joinGroup(group, NetworkInterface.getByInetAddress(InetAddress.getByName(HelloApplication.config.multicastIp))); // Deprecated in Java 16+

            System.out.println("Joined multicast group. Listening for messages...");

            // Receive packet

            byte[] buffer = new byte[1024];
            DatagramPacket packet = new DatagramPacket(buffer, buffer.length);

            socket.setSoTimeout(1000);
            thread = new Thread(() -> {
                System.out.println("Multicast thread started");
                while (!thread.isInterrupted()) {

                    try {
                        socket.receive(packet);
                        String msg = new String(packet.getData(), 0, packet.getLength());
                        HelloApplication.queues.get(cwp.id).add(msg);
                    } catch (Exception e) {
                        HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());
                    }

//                    System.out.println("Received: " + msg);
                }


                System.out.println("interrupted");

            });

            (thread).start();
//            thread.start();
        } catch (Exception e) {
            HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());


        }


    }

    public void close() {
        try {
            if (socket != null && socket.isConnected()) {
                thread.interrupt();
                socket.leaveGroup(InetAddress.getByName(HelloApplication.config.multicastGroup));
                System.out.println("Leaving multicast group. Listening for messages...");
            }
        } catch (IOException e) {
            HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());

        } finally {
            if (socket != null && !socket.isClosed()) {
                socket.close();
            }
        }
    }
}

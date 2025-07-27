package com.avithon.neurocontrolapp;

import com.fasterxml.jackson.core.JsonProcessingException;
import com.fasterxml.jackson.core.type.TypeReference;
import javafx.application.Platform;
import javafx.beans.property.SimpleStringProperty;
import javafx.beans.property.StringProperty;
import javafx.fxml.FXML;
import javafx.fxml.FXMLLoader;
import javafx.fxml.Initializable;
import javafx.geometry.Pos;
import javafx.scene.Parent;
import javafx.scene.Scene;
import javafx.scene.control.Label;
import javafx.scene.input.MouseButton;
import javafx.scene.layout.FlowPane;
import javafx.scene.layout.HBox;
import javafx.scene.layout.VBox;
import javafx.stage.Modality;
import javafx.stage.Stage;
import org.apache.hc.client5.http.classic.methods.HttpGet;
import org.apache.hc.client5.http.impl.classic.CloseableHttpClient;
import org.apache.hc.client5.http.impl.classic.HttpClients;
import org.apache.hc.core5.http.ClassicHttpResponse;

import java.io.IOException;
import java.net.URL;
import java.util.*;
import java.util.concurrent.LinkedBlockingQueue;
import java.util.logging.Level;

public class HelloController implements Initializable {


    public Thread thread;

    @FXML
    protected FlowPane fpCwps;

    private List<Cwp> cwps = new ArrayList<>();

    List<Stage> activeStages = new ArrayList<>();


    protected void cwpClicked(Cwp cwp) {

        if (HelloApplication.queues.containsKey(cwp.id)) {
            return;
        } else {
            HelloApplication.queues.put(cwp.id, new LinkedBlockingQueue<>());
        }
        MulticastConnection multicastConnection = new MulticastConnection();
        HelloApplication.multicastConnections.add(multicastConnection);
        multicastConnection.connect(cwp);
        if (cwp.status.get().equals("DISCONNECTED")) {
            return;
        }
        System.out.println(cwp + " clicked");
        FXMLLoader loader = new FXMLLoader(getClass().getResource("popup.fxml"));
        Parent root = null;
        try {
            root = loader.load();
        } catch (IOException e) {
            HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());

        }
        PopupController popupController = loader.getController();
        popupController.addCwp(cwp.clone());
        Stage popupStage = new Stage();
        popupStage.initModality(Modality.WINDOW_MODAL); // block events to other windows
        popupStage.setTitle("Live EEG Signal");
        popupStage.setWidth(1280);
        popupStage.setHeight(800);
        popupStage.setMinWidth(1280);
        popupStage.setMinHeight(800);
        popupStage.setIconified(false);
        popupStage.setScene(new Scene(root));
        popupStage.setUserData(popupController);
        activeStages.add(popupStage);

        popupStage.setOnCloseRequest((event1) -> {
            activeStages.remove(popupStage);
            multicastConnection.close();
            popupController.thread.interrupt();
            HelloApplication.queues.remove(cwp.id);
        });
        popupStage.show();


    }

    private FlowPane getCard(FlowPane source) {
        return ((FlowPane) source.getChildren().get(0));
    }

    private Label getLabelFromCard(FlowPane card) {
        return (Label) card.getChildren().get(0);
    }

    private VBox getParentCard(FlowPane card) {
        return ((VBox) card.getParent());
    }

    @Override
    public void initialize(URL url, ResourceBundle resourceBundle) {

        createCwps();
        // Start the task on a background thread
        thread = new Thread(() -> {
            while (!thread.isInterrupted()) {
                try(CloseableHttpClient client = HttpClients.createDefault()) {

                        HttpGet request = new HttpGet("http://" + HelloApplication.config.apiIp + ":" + HelloApplication.config.apiPort + "/state");
                        ClassicHttpResponse response = client.executeOpen(null, request, null);
                        String result = new String(response.getEntity().getContent().readAllBytes());

//                    HttpClient client = HttpClient.newHttpClient();
//                    HttpRequest request = HttpRequest.newBuilder()
//                            .timeout(Duration.ofMillis(1000))
//                            .uri(URI.create("http://" + HelloApplication.config.apiIp + ":" + HelloApplication.config.apiPort + "/state"))
//                            .GET()
//                            .build();
//
//                    HttpResponse<String> response = client.send(request, HttpResponse.BodyHandlers.ofString());

                    Platform.runLater(() -> updateState(result));
                    ;
                    Thread.sleep(2000);
                } catch (InterruptedException e) {
                    HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());
                    break;
                } catch (IOException e) {
                    HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());
                }
            }
        });
        thread.start();

    }

    private void createCwps() {
        Cwp cwp1 = new Cwp("OEAA", "CWP 1", "DISCONNECTED");
        Cwp cwp2 = new Cwp("OEBB", "CWP 2", "DISCONNECTED");
        Cwp cwp3 = new Cwp("OECC", "CWP 3", "DISCONNECTED");
        Cwp cwp4 = new Cwp("OEDD", "CWP 4", "DISCONNECTED");
        Cwp cwp5 = new Cwp("OEEE", "CWP 5", "DISCONNECTED");
        Cwp cwp6 = new Cwp("OEFF", "CWP 6", "DISCONNECTED");
        Cwp cwp7 = new Cwp("OEGG", "CWP 7", "DISCONNECTED");
        Cwp cwp8 = new Cwp("OEHH", "CWP 8", "INACTIVE");
        Cwp cwp9 = new Cwp("OEII", "CWP 9", "DISCONNECTED");
        Cwp cwp10 = new Cwp("OEJJ", "CWP 10", "DISCONNECTED");
        HBox hbCwps1 = new HBox();
        cwps = List.of(cwp1,
                cwp2,
                cwp3,
                cwp4,
                cwp5,
                cwp6,
                cwp7

        );
        hbCwps1.setSpacing(10);
        hbCwps1.getChildren().addAll(
                cwp1.vbMain,
                cwp2.vbMain,
                cwp3.vbMain,
                cwp4.vbMain,
                cwp5.vbMain
        );

        hbCwps1.setAlignment(Pos.CENTER);
        HBox hbCwps2 = new HBox();
        hbCwps2.setSpacing(10);
        hbCwps2.getChildren().addAll(
                cwp6.vbMain,
                cwp7.vbMain,
                cwp8.vbMain,
                cwp9.vbMain,
                cwp10.vbMain
        );

        for (Cwp cwp : cwps) {
            cwp.fpCard.setOnMouseClicked(mouseEvent -> {
                if (mouseEvent.getButton().equals(MouseButton.PRIMARY)) {
                    cwpClicked(cwp);
                }
            });
        }

        hbCwps2.setAlignment(Pos.CENTER);
        fpCwps.getChildren().addAll(hbCwps1, hbCwps2);
    }

    private void updateState(String json) {
        try {
            HashMap<String, Object> map = HelloApplication.objectMapper.readValue(json, new TypeReference<HashMap<String, Object>>() {
            });
            for (Cwp cwp : cwps) {

                String state = map.get("state_of_mind").toString().toUpperCase();
                cwp.setStatus(state);
                HashMap<String, String> states = new HashMap<>();
                for (Map.Entry<String, Object> entry: map.entrySet()){
                    if (entry.getKey().startsWith("state") && !entry.getKey().startsWith("state_") && entry.getValue().toString().contains(": ")){
                        states.put(entry.getKey(), entry.getValue().toString());
                    }
                }
                cwp.setStates(states);
            }


        } catch (JsonProcessingException e) {
            HelloApplication.LOGGER.log(Level.SEVERE,e.getMessage());

        }

    }


}
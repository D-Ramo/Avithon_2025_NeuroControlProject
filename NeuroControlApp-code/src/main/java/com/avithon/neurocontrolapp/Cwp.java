package com.avithon.neurocontrolapp;

import javafx.beans.property.SimpleStringProperty;
import javafx.beans.property.StringProperty;
import javafx.geometry.Orientation;
import javafx.geometry.Pos;
import javafx.scene.control.Label;
import javafx.scene.layout.FlowPane;
import javafx.scene.layout.VBox;
import javafx.scene.paint.Paint;
import javafx.scene.text.Font;
import javafx.scene.text.FontWeight;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.UUID;
public class Cwp {
    String id;
    StringProperty sector = new SimpleStringProperty();
    StringProperty name = new SimpleStringProperty();
    StringProperty status = new SimpleStringProperty();
    HashMap<String,StringProperty> states = new HashMap<>();
    HashMap<String,StringProperty> statesValues = new HashMap<>();

    VBox vbMain = new VBox();
    FlowPane fpSector = new FlowPane();
    FlowPane fpCard = new FlowPane();

    Label lblSector = new Label();

    FlowPane fpStatus = new FlowPane();
    FlowPane fpName = new FlowPane();

    Label lblStatus = new Label();
    Label lblName = new Label();


    Cwp(String sector, String name, String status) {
        id = UUID.randomUUID().toString();
        this.sector.set(sector);
        this.name.set(name);
        setStatus(status);


//        fpCard.setMaxWidth(200);
//        fpSector.setMaxWidth(200);
//        fpName.setMaxWidth(200);
//        fpStatus.setMaxWidth(200);



        lblSector.textProperty().bindBidirectional(this.sector);
        lblSector.setFont(Font.font(18));

        lblName.textProperty().bindBidirectional(this.name);

        lblStatus.textProperty().bind(this.status);
        lblStatus.setFont(Font.font("Segoe UI", FontWeight.BOLD, 16));
        lblStatus.setTextFill(Paint.valueOf("white"));

        fpSector.setAlignment(Pos.CENTER);
        fpSector.setPrefHeight(40);
        fpSector.getChildren().add(lblSector);


        fpStatus.setAlignment(Pos.CENTER);
        fpStatus.getChildren().add(lblStatus);
        fpStatus.setPrefWidth(190);
        fpStatus.setPrefHeight(40);

        fpName.setAlignment(Pos.CENTER);
        fpName.getChildren().add(lblName);
        fpName.setPrefWidth(190);
        fpName.setPrefHeight(40);

        fpCard.setStyle(
                "-fx-border-color:  lightgray; " +
                "-fx-border-width: 1px; " +
                "-fx-border-radius: 12px; " +
                "-fx-background-radius: 12px; " +
                "-fx-background-color: white; " +
                "-fx-cursor: hand; " +
                "-fx-padding: 10px 10px 10px 10px; ");
        fpCard.setOrientation(Orientation.VERTICAL);
        fpCard.getChildren().addAll(fpStatus, fpName);
        fpCard.setPrefSize(212, 105);

        vbMain.setPrefWidth(200);
        vbMain.setMaxWidth(200);
//        vbMain.setPrefHeight(145);
//        vbMain.setMaxHeight(145);
        vbMain.setMaxWidth(vbMain.getPrefWidth());
        vbMain.getChildren().addAll(fpSector, fpCard);
    }

    private VBox getVbMain() {
        return vbMain;
    }

    public Cwp clone() {
        Cwp clone = new Cwp(sector.get(), name.get(), status.get());
        clone.id = id;
        clone.fpStatus.styleProperty().bind(fpStatus.styleProperty());
        clone.lblStatus.textProperty().bind(lblStatus.textProperty());
        clone.states = this.states;
        clone.statesValues = this.statesValues;
        return clone;
    }

    public void setStatus(String state) {
        if (state.equals("INACTIVE")) {
            fpStatus.setStyle("-fx-background-color: #2ECC71; -fx-background-radius: 10;");
        }else if (state.equals("ACTIVE")) {
            fpStatus.setStyle("-fx-background-color: #E74C3C; -fx-background-radius: 10;");
        }else if (state.equals("DISCONNECTED")) {
            fpStatus.setStyle("-fx-background-color: #BDC3C7; -fx-background-radius: 10; ");
        }
        status.set(state);
    }

    public void setStates(HashMap<String, String> states) {
        for (String key : states.keySet()) {
            String [] splited = states.get(key).split(": ");
//            if (splited[1].contains("N/A")){
//                splited[1] = 3.2+"";
//            }
//            if (splited[1].trim().equals("0")){
//                splited[1] = 7+"";
//            }
            if (this.states.containsKey(key)) {

                this.states.get(key).set(splited[0]);
                this.statesValues.get(key).set(splited[1]);
            }else {
                this.states.put(key,new SimpleStringProperty(states.get(splited[0])));
                this.statesValues.put(key,new SimpleStringProperty(states.get(splited[1])));
            }
        }
    }
}

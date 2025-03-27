import {Component, NgZone, OnDestroy} from '@angular/core';

import { SpiderEyeService } from '../../services/spidereye.service';

import { SomeDataModel } from '../../models/some-data-model';
import { PowerModel } from '../../models/power-model';
import {Subscription} from "rxjs";
import {MessageBox, SpiderEye} from "spidereye";

@Component({
    selector: 'app-bridge',
    templateUrl: './bridge.component.html',
    styleUrls: ['./bridge.component.scss']
})
export class BridgeComponent implements OnDestroy {

    longRunningTaskState: string;
    longRunningState: string;
    getDataState: string;
    getInstanceState: string;
    powerState: string;
    getErrorState: string;
    promptState: string;

    powerValue = 2;
    powerPower = 4;
    
    counter = 0;
    
    private readonly showMessageSubscription: Subscription;

    constructor(private spidereye: SpiderEyeService, private ngZone: NgZone) {
        this.showMessageSubscription = spidereye.registerHandler<string>('IUiBridgeClientService.ShowMessage')
            .subscribe(msg => this.showMessage(msg));

        // add raw async handler
        SpiderEye.addEventHandler<string, string>("IUiBridgeClientService.Prompt", msg => new Promise(resolve => {
            this.ngZone.run(() => this.promptState = "working...");
            setTimeout(() => resolve("prompted: " + msg + " => " + this.counter++), 1000);
        }));
    }

    public ngOnDestroy(): void {
        this.showMessageSubscription.unsubscribe();
    }

    startLongRunningTask() {
        this.longRunningTaskState = 'Running...';
        this.spidereye.invokeApi<void>('UiBridge.runLongProcedureOnTask')
            .subscribe(() => this.longRunningTaskState = 'Done!',
                error => this.longRunningTaskState = 'Error: ' + error.message);
    }

    startLongRunning() {
        this.longRunningState = 'Running...';
        this.spidereye.invokeApi<void>('UiBridge.runLongProcedure')
            .subscribe(() => this.longRunningState = 'Done!',
                error => this.longRunningState = 'Error: ' + error.message);
    }

    getData() {
        this.getDataState = 'Getting...';
        this.spidereye.invokeApi<SomeDataModel>('UiBridge.getSomeData')
            .subscribe(data => this.getDataState = 'Result: ' + JSON.stringify(data),
                error => this.getDataState = 'Error: ' + error.message);
    }

    getInstanceId() {
        this.getInstanceState = 'Getting...';
        this.spidereye.invokeApi<string>('UiBridge.getInstanceId')
            .subscribe(data => this.getInstanceState = 'Result: ' + data,
                error => this.getInstanceState = 'Error: ' + error.message);
    }

    power() {
        const parameters: PowerModel = {
            value: this.powerValue,
            power: this.powerPower,
        };

        this.powerState = 'Calculating...';
        this.spidereye.invokeApi<number, PowerModel>('UiBridge.power', parameters)
            .subscribe(msg => this.powerState = 'Result: ' + msg,
                error => this.powerState = 'Error: ' + error.message);
    }

    getError() {
        this.getErrorState = 'Throwing...';
        this.spidereye.invokeApi<void>('UiBridge.produceError')
            .subscribe(() => this.getErrorState = 'Everything went well, that shouldn\'t happen here',
                error => this.getErrorState = 'Thrown Error: ' + error.message);
    }
    
    callShowMessage() {
        this.spidereye.invokeApi<void>('UiBridge.callShowMessage');
    }
    
    callPrompt() {
        this.spidereye.invokeApi<string>('UiBridge.callPrompt')
            .subscribe(msg => this.promptState = msg);
    }
    
    showMessage(msg: string) {
        const msgBox = new MessageBox();
        msgBox.message = msg;
        msgBox.showAsync();
    }
}

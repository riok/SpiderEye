import { Component, OnInit } from '@angular/core';
import {SpiderEyeService} from "../../services/spidereye.service";

@Component({
    selector: 'app-embed',
    templateUrl: './embed.component.html',
    styleUrls: ['./embed.component.scss']
})
export class EmbedComponent implements OnInit {

    imageUrl: string;
    pdfUrl: string;
    
    constructor(private spidereye: SpiderEyeService) {
    }
    
    ngOnInit(): void {
        this.spidereye.invokeApi<string>('UiBridge.getCustomFileHost')
            .subscribe(host => {
                this.imageUrl = host + 'logo.png';
                this.pdfUrl = host + 'dummy.pdf';
            });
    }
}

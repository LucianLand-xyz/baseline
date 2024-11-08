import RCCClient from "@megahdrive/rccclient-node";
import ScriptExecution from "@megahdrive/rccclient-node/ScriptExecution";
import Job from "@megahdrive/rccclient-node/Job";
import { v4 } from "uuid";

const HatRender = await Bun.file("HatRender.lua").text();
const Service = new RCCClient("127.0.0.1", 6969)

const RendererJob = new Job(`Renderer-Job-${v4()}`)
const Script = new ScriptExecution("Renderer", HatRender)

Service.BatchJobEx(RendererJob, Script, (result => {
    if (result) {
        console.log("Job executed successfully!")
        console.log("Result: ", result)
    } else {
        console.log("BatchJobEx failed!")
    }
}))

Forked from https://github.com/openai/shap-e
### Run API

```shell
# Run this command from a shell at shap-e to start the api.
uvicorn main:app --reload

uvicorn main:app --reload --host 0.0.0.0 --port 80

#euler
srun --ntasks=1 --gpus=1 --time=2:00:00 --mem-per-cpu=5000 --pty bash

env2lmod

sbatch .loadmodules

hostname -i #10.204.97.75

uvicorn main:app --reload --host 127.0.0.1 --port 8000

ssh <user>@euler.ethz.ch -L 8080:<ip>:8080 -N &

ssh -L 8080:10.204.97.75:8080 winklerr@euler.ethz.ch -N

ssh winklerr@euler.ethz.ch -L 8080:129.132.93.117:8080
#ssh winklerr@euler.ethz.ch -L 8000:10.204.97.66:8000 -N
#copy file from euler
scp <username>@euler.ethz.ch:<path> .

scp winklerr@euler.ethz.ch:wish3d/app/slurm-33867502.out .

Load required modules
```

Endpoint will be available at http://localhost:8000/shape

there are 2 query parameters, prompt & nr_samples

prompt accepts a string prompt to give to the model.

nr_samples is optional & defines the number of samples the model will create for the prompt.
default is 3
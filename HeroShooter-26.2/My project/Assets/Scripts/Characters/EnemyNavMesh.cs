using UnityEngine;
using UnityEngine.AI;

// Cuida somente da navegação dos inimigos.
// Os controladores de Soldier/Berserker decidem para onde ir; este script move o NavMeshAgent.
public class EnemyNavMesh : MonoBehaviour
{
    [Header("Movimento")]
    public NavMeshAgent agent;
    public float moveSpeed = 4f;
    public float turnSpeed = 360f;

    void Awake()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (agent == null)
        {
            Debug.LogError("EnemyNavMesh precisa de um NavMeshAgent: " + name);
            enabled = false;
            return;
        }

        // A rotação será feita por este script, para o inimigo olhar na direção do movimento.
        agent.speed = moveSpeed;
        agent.updateRotation = false;
        agent.stoppingDistance = 0f;
    }

    public bool IsReady()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    // Envia o agente até uma posição do cenário.
    public void MoveTo(Vector3 destination)
    {
        if (!IsReady()) return;

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    // Faz o inimigo parar de andar, mas mantém o agente ativo.
    public void Stop()
    {
        if (IsReady())
            agent.isStopped = true;
    }

    // Atualiza a velocidade pelo Inspector ou por outro script.
    public void SetSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        if (agent != null)
            agent.speed = moveSpeed;
    }

    // Gira o personagem para olhar na direção informada, ignorando a altura.
    public void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
            return;

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            Quaternion.LookRotation(direction),
            turnSpeed * Time.deltaTime);
    }

    // Gira na direção para onde o NavMeshAgent está andando.
    public void FaceVelocity()
    {
        if (IsReady())
            FaceDirection(agent.velocity);
    }

    // Verifica se o inimigo chegou perto do destino atual.
    public bool ReachedDestination(float distance = 0.5f)
    {
        if (!IsReady() || agent.pathPending)
            return false;

        return agent.remainingDistance <= distance;
    }

    // Procura um ponto válido do NavMesh próximo de uma posição desejada.
    public bool TryGetNavMeshPoint(Vector3 desiredPosition, float searchRadius, out Vector3 result)
    {
        if (NavMesh.SamplePosition(
            desiredPosition, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
        {
            result = hit.position;
            return true;
        }

        result = desiredPosition;
        return false;
    }

    // Sorteia um ponto próximo que esteja sobre o NavMesh.
    public bool TryGetRandomPoint(Vector3 center, float radius, out Vector3 result)
    {
        for (int i = 0; i < 8; i++)
        {
            Vector3 randomPoint = center + Random.insideUnitSphere * radius;

            if (TryGetNavMeshPoint(randomPoint, radius, out result))
                return true;
        }

        result = transform.position;
        return false;
    }
}
